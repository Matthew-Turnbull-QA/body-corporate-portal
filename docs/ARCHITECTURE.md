# Architecture decisions — Phase 1 (Auth + Users)

This records the decisions with real trade-offs, so they aren't silently forgotten.
See the top-level `README.md` for setup/run instructions.

## Solution layout

Clean Architecture: `Bcmp.Domain` (entities/enums, no dependencies) → `Bcmp.Application`
(use-case services, interfaces, DTOs) → `Bcmp.Infrastructure` (EF Core, Google/JWT
implementations) → `Bcmp.Api` (ASP.NET Core host, composition root). `Infrastructure`
implements interfaces defined in `Application` (dependency inversion) — e.g.
`IUserRepository`, `IGoogleTokenValidator`, `IJwtTokenGenerator` all live in
`Application`, and only their concrete implementations live in `Infrastructure`.

Monorepo with `/backend` and `/frontend` as siblings, each with its own toolchain,
CI workflow (path-filtered), and deploy target.

## Authentication: Google ID token → app-issued JWT, gated by the Users table

Google is used purely as a one-time identity *verifier*, not as the ongoing session
mechanism. Flow: React gets a Google ID token client-side → posts it to
`POST /api/auth/google` → `GoogleTokenValidator` validates signature/audience/issuer
→ `AuthenticationService` looks up the verified email in `Users` → **rejects before
issuing anything** if the email is unknown or disabled → only then issues the app's
own short-lived JWT. No user is ever auto-created; this is the enforcement point for
"no self-registration."

An unknown email and a disabled user get the *same* rejection response deliberately —
the app never reveals whether a given email exists, which would otherwise leak
membership information to an unauthenticated caller.

## Token storage: bearer JWT in memory, not a cookie

**Decision:** the SPA holds the JWT in React state (in memory only, no `localStorage`/
`sessionStorage`) and attaches it via the `Authorization` header.

**Why:** frontend (Vercel/Netlify) and backend (Render) live on different domains even
in production, since both are free-tier services on their own subdomains. Cross-domain
`SameSite=None; Secure` cookies are fussy to get right and increasingly restricted by
browsers (Safari ITP, third-party cookie deprecation). A bearer token sidesteps this
entirely — it's just a header on a `fetch` call; CORS only needs to allow the specific
origin, no `credentials: include` complexity.

**Trade-off accepted:** an in-memory token is more exposed to XSS than an `httpOnly`
cookie (JS *can* read it, unlike a cookie the browser hides from JS). Mitigated with a
short token lifetime (`Auth:Jwt:ExpiryHours`, default 8h) and standard React XSS
hygiene (default JSX escaping, no `dangerouslySetInnerHTML` with unsanitized input). A
side effect worth knowing: refreshing the browser tab loses the session (no persistence,
no refresh-token mechanism in Phase 1) — the user has to sign in again. Acceptable for
this app's usage pattern (a handful of trustees, not a high-frequency consumer app); can
be revisited later if it becomes a real UX complaint.

## Role claim: plain `"role"`, not `ClaimTypes.Role`

Found live, not in review: `JwtSecurityTokenHandler`'s default *outbound* claim mapping
silently rewrites `ClaimTypes.Role` (the long XML-schema URI) to a short `"role"` claim
when writing a token, but `AddJwtBearer`'s `TokenValidationParameters.RoleClaimType` was
initially set to that same long URI for *validation* — so every `RequireRole` check
failed even for a correctly-signed Administrator token, because the claim the validator
was told to look for never actually appeared in the issued token. Fixed by using the
plain `"role"` string as the claim type on both the issuing (`JwtTokenGenerator`) and
validating (`Program.cs`) sides, with `MapInboundClaims = false` so nothing gets
silently remapped in either direction.

## Migrations: applied explicitly, not on app boot

`dotnet ef database update` is a manual/CI step, not `Database.Migrate()` called from
`Program.cs`. On a free-tier host with cold-start-driven restarts, an on-boot migration
risks two instances (or a retry) racing to apply the same migration concurrently. This
is a deliberate omission, not an oversight — revisit only if a proper deploy pipeline
with a single migration step is added later.

## Logging: console-only Serilog sink

No file sink, unlike a desktop-app context. Free-tier hosting containers have ephemeral
disks — anything written to a local file is lost on the next redeploy or restart, so a
file sink would just be dead weight. Console output is what Render/Fly's log
aggregation actually captures.

## CORS

Found live, not in review: there was no CORS configuration at all until it was added —
every cross-origin call from the Vite dev server to the API hung silently (no error
surfaced to the console beyond the browser's own network tab). This wasn't just a local
dev nuisance to patch around; frontend and backend are cross-origin in production too,
so it was a genuine Phase 1 gap. `Cors:AllowedOrigins` (array, via config/env) drives an
explicit allow-list — nothing is allowed by default in Production; local dev gets a
committed (non-secret) default of `http://localhost:5173` via `appsettings.Development.json`.

## EF Core + immutable records

`User` is a `sealed record` — updates go through `with { ... }` to produce a new
instance, never in-place mutation. This collides with EF Core's `Update()`, which tries
to attach a *second* tracked instance with the same key when the original (from an
earlier `GetByIdAsync` in the same request) is still tracked on the same `DbContext`.
`UserRepository.UpdateAsync` checks the change tracker for an existing entry and calls
`CurrentValues.SetValues(...)` on it instead of blindly calling `Update()` — found by
actually exercising disable-after-fetch through the live API, not by inspection.
