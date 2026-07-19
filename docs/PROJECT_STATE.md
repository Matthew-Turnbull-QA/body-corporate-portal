# Project State and Handoff

Last updated: 2026-07-19

## Current milestone

Phase 1 authentication and user-management work is complete and verified locally. The local OAuth sign-in flow is working end to end in a real browser.

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

1. Properties domain
   - Add property entities and basic CRUD
   - Expose REST endpoints in the API
   - Add a simple frontend screen for managing properties

2. Jobs domain
   - Introduce the primary job entity
   - Create a pluggable job-source abstraction so email-based creation can be added later without refactoring

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

Start implementing the Properties feature end to end:

- backend domain model
- backend application service and repository interface
- infrastructure persistence and EF migration
- API controller
- frontend list/create/edit experience

## Update log

- 2026-07-19: Local auth setup completed and verified; project state file created
- 2026-07-19: Properties domain, API, migration, and frontend screen implemented and verified
- 2026-07-19: Login flow verified end to end; add-user and add-property flows confirmed working
