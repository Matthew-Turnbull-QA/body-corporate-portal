# Project State and Handoff

Last updated: 2026-07-29

## Current milestone

Phase 1 authentication and user-management work is complete and verified locally. The local OAuth sign-in flow is working end to end in a real browser. Phase 2's Jobs domain (backend + frontend) is implemented and has had multiple rounds of real user feedback applied: sortable columns, last-updated tracking, Active/Closed split, assignable trustee field, status-change notes, and status audit history.

## Completed work

- [x] Local PostgreSQL 16 installed and running
- [x] Local database created for the app: `bcmp_dev`
- [x] Backend secrets configured for local development
- [x] EF Core migration applied successfully
- [x] Bootstrap admin user seeded
- [x] Backend API running locally on `http://localhost:5151`
- [x] Frontend running locally on `http://localhost:5173`
- [x] Google OAuth sign-in verified successfully in a browser

## Current status summary

Phase 2 is underway: Properties and Jobs are both built, with Jobs having gone through real user feedback. The 2026-07-29 suggested-changes batch added configurable per-user job workflow permissions, optional local email/password login alongside Google sign-in, and an admin-reviewed request-access flow. The foundation remains stable: auth, user management, database access, and local dev flow all still work.

## Recommended next priority

Confirm local email/password sign-in edge cases (see "Immediate next action" below), then move on to the Jobs domain's remaining feature gaps. Email integration is next in the Phase 2 sequence, though the Assignment engine now has a manual-assign foundation already in place if you'd rather build routing/notifications on top of that first instead.

### Suggested Phase 2 sequence

1. Properties domain - done.

2. Jobs domain - done (2026-07-20), plus feedback rounds on 2026-07-20 and 2026-07-29.
   `Job` entity (title, description, status, source, property FK, plus `UpdatedAtUtc` and nullable `AssignedTrusteeUserId`), full Domain -> Application -> Infrastructure -> Api layering matching Properties, four EF migrations (`AddJobs`, `AddJobUpdatedAtUtc`, `AddJobAssignedTrustee`, `AddJobStatusHistory`), and a frontend list screen with an add-job dialog, sortable columns, and two sections: Active (`Open`, `InProgress`) and Closed (`Completed`, `Cancelled`).

   The pluggable part: `JobService.CreateJobAsync` takes a `JobSource` parameter (only `Manual` is produced today); a future email-ingestion worker becomes a second caller of that same method with `Source.Email`, not a refactor of it.

   Trustee assignment (updated 2026-07-29): `PATCH /api/jobs/{id}/assign` is now controlled by the user's `AssignJobs` permission instead of hard-coded Administrator-only access. The target user still needs to exist and be `Role.Trustee` (400 if not, 404 if unknown). This is groundwork for Phase 2 item 4 (Assignment engine) below, not that engine itself; right now it's a manual per-job dropdown, not routing/notification logic.

   User workflow permissions (new 2026-07-29): Users now carry configurable job permissions: `LoadJobs`, `CreateJobs`, `UpdateJobStatus`, and `AssignJobs`. Existing Trustees keep load/create/status-update by default; existing Administrators get all four via migration.

   Local password login (new 2026-07-29): Users can optionally have a local password hash set by an Administrator when creating/editing the user. `/api/auth/password` signs in with email/password while Google sign-in remains available.

   Request access (new 2026-07-29): The login screen now links to a public request-access form. Anonymous visitors can submit contact/property details into `AccessRequests`, but no login-capable user is created until an Administrator approves the request, chooses role/permissions, and optionally sets a local password. If the submitted email already belongs to a user, the request links to that user and appears in the Access tab as a reactivation or existing-account card for admin handling instead of trying to create a duplicate user.

   Status audit history (new 2026-07-29): Job status changes now require a confirmation modal with optional notes. Each transition writes a `JobStatusHistory` row with from/to status, note, actor, and timestamp. Admins can edit the history note text in-place from the expanded row history; status values, actor, and original timestamp stay immutable, with note edit actor/timestamp tracked separately. Normal users with `LoadJobs` can view history but cannot edit notes.

   Verified live against local Postgres/API for every increment (create, list with joined property/trustee names, status transitions, assign/unassign, the 400/401/403/404 cases). After status audit history landed, 22/22 domain tests and 47/47 application tests pass. `npm run build` and `npm run lint` are green, with the existing Fast Refresh warning in `AuthContext.tsx`. Browser click-through on 2026-07-29 confirmed: Cancelled jobs appear under Closed, status changes open a notes modal, history expands inline, admins can edit notes, and status chips/selects keep fixed sizes.

3. Email integration
   - Add inbound or outbound email handling for job creation and notifications

4. Assignment engine
   - Route jobs to the right users or teams

5. Dashboards and AI enrichment
   - Add reporting views and lightweight automation

## Implementation conventions to keep

- Follow the existing Clean Architecture boundary:
  - Domain -> Application -> Infrastructure -> API
- Keep changes small and testable
- Verify with real local runs and browser checks, not only unit tests
- Add a migration for any database schema change
- When implementing UI styles and layout, use the design feature/system in the project rather than ad-hoc styling
- Keep the handoff file updated after each completed step

## Immediate next action

1. Confirm local email/password sign-in with a user that has a local password set, then confirm a user without one still cannot sign in locally.
2. Then move on to the Jobs domain's remaining gaps - job editing, and whatever the next Phase 2 item needs (see roadmap above: Email integration next; Assignment engine now has a manual-assign foundation to build routing/notifications on top of).

## Update log

- 2026-07-19: Local auth setup completed and verified; project state file created
- 2026-07-19: Properties domain, API, migration, and frontend screen implemented and verified
- 2026-07-19: Login flow verified end to end; add-user and add-property flows confirmed working
- 2026-07-20: Jobs domain implemented (backend fully live-verified; frontend built/linted but not yet click-tested in a browser)
- 2026-07-20: Jobs feedback round: sortable columns, UpdatedAtUtc + "Last updated" column, Active/Completed split, and an Administrator-only assign-to-trustee field/endpoint (groundwork for the Assignment engine phase). All backend pieces live-verified; 42/42 backend tests pass.
- 2026-07-29: Suggested changes batch implemented: local OAuth origin verified as `http://localhost:5173`, configurable per-user job permissions added, and optional local email/password login added. Verified with frontend build/lint, API build, backend application/domain tests, local migration, and local dev-server restart.
- 2026-07-29: Request-access flow implemented: public details form, admin review list, approve/reject endpoints, and approval into a real user. Verified with frontend build/lint, API build, backend application/domain tests, local migration, local dev-server restart, and user browser click-through.
- 2026-07-29: Existing-user request handling refined: access requests now store `ExistingUserId` when the email already belongs to a user, allowing admins to reactivate disabled users or handle active-account follow-ups from the Access tab. Verified with frontend build/lint, backend application tests, local migration, local dev-server restart, and user browser click-through. The duplicate pending local row for `automationtoolsmith@gmail.com` was manually removed from `bcmp_dev`, leaving the approved request/user intact.
- 2026-07-29: Jobs click-test fixes implemented: Cancelled now sits under Closed instead of Active, status controls have fixed sizing, status changes require a notes modal, and `JobStatusHistory` tracks transitions with admin-editable notes. Verified with backend build, local migration, 22/22 domain tests, 47/47 application tests, frontend build/lint, backend health check, and Chrome click-through on `/jobs`.
