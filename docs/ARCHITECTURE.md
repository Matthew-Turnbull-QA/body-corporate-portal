# Architecture decisions

This records the decisions with real trade-offs, so they are not silently forgotten. See the top-level `README.md` for setup/run instructions and `docs/PROJECT_STATE.md` for current roadmap state.

## Solution layout

Clean Architecture: `Bcmp.Domain` (entities/enums, no dependencies) -> `Bcmp.Application` (use-case services, interfaces, DTOs) -> `Bcmp.Infrastructure` (EF Core, Google/JWT implementations) -> `Bcmp.Api` (ASP.NET Core host, composition root). Infrastructure implements interfaces defined in Application, for example `IUserRepository`, `IGoogleTokenValidator`, and `IJwtTokenGenerator`.

The monorepo keeps `/backend` and `/frontend` as siblings, each with its own toolchain, CI workflow, and deploy target.

## Authentication

Google is used as a one-time identity verifier, not as the ongoing session mechanism. Flow: React obtains a Google ID token -> posts it to `POST /api/auth/google` -> the API validates signature/audience/issuer -> `AuthenticationService` looks up the verified email in `Users` -> unknown or disabled users are rejected before a token is issued -> the API issues its own short-lived JWT.

Local email/password login is optional per trustee. An enabled user with a stored password hash can sign in through `POST /api/auth/password`; users without a local password can still use Google if their email is provisioned.

The public request-access flow preserves "no self-registration." Anonymous visitors can submit contact/property details into `AccessRequests`, but no login-capable user is created until a portal admin trustee approves the request, optionally grants portal-admin access, and optionally sets a local password. Unknown email and disabled-user sign-in failures deliberately return the same rejection shape to avoid membership enumeration.

## Token storage and claims

The SPA stores the JWT in React state only and sends it via the `Authorization` header. This avoids cross-domain cookie complexity for separate free-tier frontend/backend hosts, at the accepted cost that refreshing the browser loses the session.

All enabled users are trustees. Portal administration is a boolean `IsPortalAdmin` flag, not a separate role. JWTs carry a plain `portal_admin=true|false` claim, and API policies check that claim for portal-admin screens/actions. `MapInboundClaims = false` stays enabled so ASP.NET does not rewrite claim names implicitly.

## Migrations and hosting

Migrations are applied explicitly with `dotnet ef database update`, not automatically on app boot. On free-tier hosting, on-boot migrations risk concurrent cold-start instances racing each other.

Serilog logs to console only. Hosted container disks are ephemeral, so file logging would be noisy without giving durable diagnostics.

CORS is configured through `Cors:AllowedOrigins`. Frontend and backend are cross-origin even in production, so the API uses an explicit allow-list; local development defaults to `http://localhost:5173`.

## EF Core and records

Domain entities are immutable records, so updates usually create a new instance with `with { ... }`. `UserRepository.UpdateAsync` and `JobRepository.UpdateAsync` handle EF Core tracking carefully so an already-tracked instance does not collide with the replacement instance for the same key.

## Jobs domain

Job creation goes through one method parameterized by `JobSource`. Manual creation uses `JobSource.Manual`; future email ingestion should call the same method with `JobSource.Email` instead of adding a parallel creation path.

Every enabled trustee can view and create jobs. `JobService.CreateJobAsync` auto-assigns new jobs by deterministic round robin across enabled trustees ordered by `CreatedAtUtc`, then `Id`; portal admins are included in that rotation. If no prior assigned job points at an eligible trustee, the first eligible trustee gets the next job.

Editing job details, changing status, and editing status-history notes require either `IsPortalAdmin` or being the job's current assigned trustee. Manual reassignment is reserved for portal admins and validates that the target user exists and is enabled. This is still not the full Assignment engine from the roadmap: there are no routing rules or notifications yet.

`Job.UpdatedAtUtc` is bumped in the service layer for mutating job operations (`UpdateJobAsync`, `UpdateStatusAsync`, and `AssignTrusteeAsync`) using the same `TimeProvider` used for creation timestamps.

The Jobs UI groups `Open` and `InProgress` under Active, and `Completed` and `Cancelled` under Closed.

Every job has a generated human-readable `JobNumber` such as `BCMP-000123`, backed by a PostgreSQL sequence. `PropertyId` is nullable so email-created jobs can enter the system before a trustee/admin selects the correct unit. Manual job creation still requires a property, and `JobService.UpdateStatusAsync` blocks any move away from `Open` until a property/unit has been selected.

## Assignment engine MVP

The first Assignment engine release replaces create-time round robin with a small ordered rules engine, while keeping round robin as the fallback. Portal admins own the rules. Normal trustees can see the resulting assignment on jobs, but cannot change routing rules.

Each rule has a name, enabled flag, priority order, target trustee, and optional match criteria. The MVP match criteria are:

- `PropertyId`: match jobs for a selected property/unit.
- `JobSource`: match `Manual` or `Email` jobs.
- Keywords: case-insensitive terms matched against job title and description.

All criteria configured on a rule must match. Rules with no criteria are not allowed in the MVP because fallback round robin already covers the catch-all case. If multiple rules match, the enabled rule with the highest priority wins. If the target trustee is disabled or a system user, the rule is skipped and the next matching enabled rule is considered.

Routing runs in two places:

- On job creation for both manual jobs and email-created jobs.
- Once when an email-created job moves from no property to a selected property, provided it has not been manually reassigned.

Manual portal-admin assignment is an override. Once a portal admin manually assigns a job, automated routing must not reassign that job unless a portal admin explicitly clears the override or manually chooses a different trustee. This means the implementation needs to track assignment provenance, for example `Rule`, `RoundRobinFallback`, or `ManualOverride`, rather than only storing `AssignedTrusteeUserId`.

When no enabled rule matches, the existing deterministic round robin remains the fallback across enabled non-system trustees ordered by `CreatedAtUtc`, then `Id`. Portal admins remain part of that rotation because they are also trustees. If no enabled non-system trustee exists, job creation should fail as it does today.

The MVP notification scope is assignment-focused:

- Notify the assigned trustee when a new job is assigned to them by a rule or by round robin.
- Notify the newly assigned trustee when a job is manually reassigned to them.
- Notify the previous trustee when a job is reassigned away from them.
- Notify portal admins only when routing cannot complete because all otherwise matching rule targets are unavailable.

The resident-facing email acknowledgement remains separate from assignment notifications. Email-intake acknowledgements continue to go to the sender; any BCC behavior can be narrowed after internal assignment notifications are in place and verified.

Not in the MVP: true unit ownership, trustee availability/leave calendars, workload balancing by open-job count, SLA escalation, multi-trustee assignment, status-change notifications, and AI-based classification. Those should be added only after the ordered rules path is stable.

## Email intake

Gmail intake is implemented as application-side polling through MailKit. IMAP and SMTP authenticate with OAuth2/XOAUTH2 using a stored refresh token, not a Gmail app password. The hosted service is disabled unless `EmailIntake:Enabled=true`; portal admins can also trigger a poll through `POST /api/email-intake/poll-now` and inspect recent processing records through `GET /api/email-intake/messages`.

Email-created jobs use the existing `JobService.CreateJobAsync` path with `JobSource.Email`, a non-login system user for audit fields, no initial property/unit, and the existing round-robin trustee assignment. The system user is marked with `User.IsSystem`, excluded from normal login, assignable trustee lists, and round-robin assignment.

Processed email metadata is stored in `EmailIntakeMessages` so the app can skip duplicates by provider key or `Message-Id` and retain failure diagnostics. On successful job creation, the app sends a Gmail acknowledgement to the original sender with subject `Body Corporate request received - Job #...`, quotes the assigned trustee as `Trustee {Name} {Surname}`, states a 24-hour response aim, and BCCs all enabled trustees.
