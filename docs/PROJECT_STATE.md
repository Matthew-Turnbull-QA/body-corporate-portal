# Project State and Handoff

Last updated: 2026-07-19

## Current milestone

Phase 1 authentication and user-management work is complete and verified locally. The local OAuth sign-in flow is working end to end in a real browser. Phase 2's Jobs domain (backend + frontend) is implemented and has since had a first round of real user feedback applied (sortable columns, last-updated tracking, active/completed split, assignable trustee field) — see the Jobs domain entry below for what's covered and what still needs a browser click-through pass.

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

The application is now in a good position to begin Phase 2 work. The foundation is stable: auth, user management, database access, and local development flow are all working.

## Recommended next priority

The next feature to build should be the core property-management domain, because it is a natural foundation for later work on jobs, assignments, and dashboards.

### Suggested Phase 2 sequence

1. Properties domain — done.

2. Jobs domain — done (2026-07-20), plus a first feedback round the same day.
   `Job` entity (title, description, status, source, property FK, plus
   `UpdatedAtUtc` and a nullable `AssignedTrusteeUserId` added on feedback),
   full Domain -> Application -> Infrastructure -> Api layering matching
   Properties, three EF migrations (`AddJobs`, `AddJobUpdatedAtUtc`,
   `AddJobAssignedTrustee`), and a frontend list screen with an add-job
   dialog, sortable columns (click any header to toggle ascending/
   descending), and two sections — Active on top, Completed below.

   The pluggable part: `JobService.CreateJobAsync` takes a `JobSource`
   parameter (only `Manual` is produced today); a future email-ingestion
   worker becomes a second caller of that same method with `Source.Email`,
   not a refactor of it.

   Trustee assignment (new): `PATCH /api/jobs/{id}/assign`, Administrator-only,
   validates the target user exists and is `Role.Trustee` (400 if not, 404 if
   unknown). This is groundwork for Phase 2 item 4 (Assignment engine) below,
   not that engine itself — right now it's a manual per-job dropdown, not
   routing/notification logic.

   Verified live against local Postgres/API for every increment (create,
   list with joined property/trustee names, status transitions, assign/
   unassign, the 400/401/403/404 cases); 42/42 backend unit tests pass.
   `npm run build`/`npm run lint` green throughout. **The frontend has not
   been click-tested by Claude in a real browser** — no headless-browser
   tooling was available in this local session (unlike the cloud sandbox
   that did Phase 1's frontend verification) — but the user has been
   actively using the running dev instance while giving feedback (visible
   via job/property data changing in Postgres during the session), so the
   core flows are very likely fine; a full pass through
   `docs/MANUAL_TEST_PLAN.md`-style checklist for Jobs still hasn't been
   explicitly recorded.

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

1. Explicitly confirm the Jobs screen end to end in a real browser (add a
   job, sort every column both directions, move a job to/from Completed and
   watch it switch sections, assign/unassign a trustee as Administrator) and
   record the result here — the groundwork is verified live at the API
   level, but a real Trustee-role click-through (not just a hand-crafted
   JWT) hasn't happened yet.
2. Then move on to the Jobs domain's remaining gaps — job editing, and
   whatever the next Phase 2 item needs (see roadmap above: Email
   integration next; Assignment engine now has a manual-assign foundation
   to build routing/notifications on top of).

## Update log

- 2026-07-19: Local auth setup completed and verified; project state file created
- 2026-07-19: Properties domain, API, migration, and frontend screen implemented and verified
- 2026-07-19: Login flow verified end to end; add-user and add-property flows confirmed working
- 2026-07-20: Jobs domain implemented (backend fully live-verified; frontend built/linted but not yet click-tested in a browser)
- 2026-07-20: Jobs feedback round: sortable columns, UpdatedAtUtc + "Last updated" column, Active/Completed split, and an Administrator-only assign-to-trustee field/endpoint (groundwork for the Assignment engine phase). All backend pieces live-verified; 42/42 backend tests pass.
