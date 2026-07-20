# Project State and Handoff

Last updated: 2026-07-19

## Current milestone

Phase 1 authentication and user-management work is complete and verified locally. The local OAuth sign-in flow is working end to end in a real browser. Phase 2's Jobs domain (backend + frontend) is now implemented; UI has not yet been click-tested in a browser (see "Immediate next action").

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

2. Jobs domain — done (2026-07-20). `Job` entity (title, description, status,
   source, property FK), full Domain -> Application -> Infrastructure -> Api
   layering matching Properties, `AddJobs` EF migration, and a frontend list
   screen with an add-job dialog and an inline status dropdown. The pluggable
   part: `JobService.CreateJobAsync` takes a `JobSource` parameter (only
   `Manual` is produced today); a future email-ingestion worker becomes a
   second caller of that same method with `Source.Email`, not a refactor of
   it. Verified live against local Postgres/API (create, list with joined
   property name, status transitions, 401/404 cases); 37/37 backend unit
   tests pass. `npm run build`/`npm run lint` green. **Not yet click-tested
   in a real browser** — no headless-browser tooling was available in this
   local session (unlike the cloud sandbox that did Phase 1's frontend
   verification), so this is unverified UI, not just untested code.

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

1. Click through the Jobs screen in a real browser (`/jobs`: add a job against
   a property, change its status via the dropdown, confirm the table
   reflects it) and record the result here.
2. Then move on to the Jobs domain's remaining gaps — job editing, and
   whatever the next Phase 2 item needs (see roadmap above: Email
   integration next).

## Update log

- 2026-07-19: Local auth setup completed and verified; project state file created
- 2026-07-19: Properties domain, API, migration, and frontend screen implemented and verified
- 2026-07-19: Login flow verified end to end; add-user and add-property flows confirmed working
- 2026-07-20: Jobs domain implemented (backend fully live-verified; frontend built/linted but not yet click-tested in a browser)
