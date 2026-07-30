# Project State and Handoff

Last updated: 2026-07-30

## Current milestone

Phase 1 authentication and user-management work is complete and verified locally. The local OAuth sign-in flow is working end to end in a real browser. Phase 2's Properties and Jobs domains are implemented. Jobs now include sortable columns, last-updated tracking, Active/Closed grouping, round-robin trustee assignment, job editing, manual portal-admin reassignment, status-change notes, and status audit history.

## Completed work

- [x] Local PostgreSQL 16 installed and running
- [x] Local database created for the app: `bcmp_dev`
- [x] Backend secrets configured for local development
- [x] EF Core migrations applied successfully
- [x] Bootstrap portal admin user seeded
- [x] Google OAuth sign-in verified successfully in a browser
- [x] Local email/password sign-in edge cases verified manually
- [x] Trustee-only job access and round-robin assignment implemented

## Current status summary

The 2026-07-30 trustee-only pivot replaced `Administrator`/`Trustee` roles and granular job permissions with a single trustee user model plus an `IsPortalAdmin` flag. Every enabled user is a trustee. Portal admins manage users, access requests, and manual job reassignment. All enabled trustees can view and create jobs. New jobs auto-assign by deterministic round robin across enabled trustees, including portal admins. Only portal admins or the assigned trustee can edit job details, change status, or edit status-history notes.

The foundation remains stable: auth, user management, database access, local password login, access requests, and local dev flow all still work.

## Recommended next priority

Click-test the trustee-only job workflow with one portal admin trustee and at least one non-admin trustee, then pick the next larger Phase 2 track: Email integration, or Assignment engine routing/notifications on top of the current round-robin/manual-assign foundation.

### Suggested Phase 2 sequence

1. Properties domain - done.

2. Jobs domain - done, with feedback rounds on 2026-07-20, 2026-07-29, and 2026-07-30.

   `Job` has title, description, status, source, property FK, `UpdatedAtUtc`, nullable `AssignedTrusteeUserId`, and status audit history. The frontend list screen has add/edit job dialogs, sortable columns, Active (`Open`, `InProgress`) and Closed (`Completed`, `Cancelled`) sections, assignment display, status-change confirmation notes, and expandable history.

   `JobService.CreateJobAsync` still takes `JobSource`, so future email ingestion can call the same creation path with `JobSource.Email`.

   The trustee-only access model uses `IsPortalAdmin`; roles and per-user job permissions have been removed. Existing Administrators were backfilled to `IsPortalAdmin = true`; existing Trustees became normal trustees.

   New jobs auto-assign to enabled trustees using deterministic round robin ordered by `CreatedAtUtc` then `Id`. Portal admins are included in the rotation. Portal admins alone can manually reassign jobs. Portal admins and assigned trustees can edit job details, change status, and edit status-history notes; other trustees can view jobs/history only.

3. Email integration
   - Add inbound or outbound email handling for job creation and notifications.

4. Assignment engine
   - Build routing/notification rules on top of the current round-robin/manual-assignment foundation.

5. Dashboards and AI enrichment
   - Add reporting views and lightweight automation.

## Implementation conventions to keep

- Follow the existing Clean Architecture boundary:
  - Domain -> Application -> Infrastructure -> API
- Keep changes small and testable
- Verify with real local runs and browser checks, not only unit tests
- Add a migration for any database schema change
- When implementing UI styles and layout, use the design feature/system in the project rather than ad-hoc styling
- Keep the handoff file updated after each completed step

## Immediate next action

1. Click-test trustee-only jobs: create several jobs, confirm round-robin assignment, confirm assigned-only edit/status controls, and confirm portal-admin-only reassignment.
2. Then choose the next larger Phase 2 track: Email integration, or Assignment engine routing/notifications.

## Update log

- 2026-07-19: Local auth setup completed and verified; project state file created.
- 2026-07-19: Properties domain, API, migration, and frontend screen implemented and verified.
- 2026-07-19: Login flow verified end to end; add-user and add-property flows confirmed working.
- 2026-07-20: Jobs domain implemented and backend live-verified.
- 2026-07-20: Jobs feedback round added sortable columns, `UpdatedAtUtc`, Active/Completed grouping, and administrator-only manual trustee assignment.
- 2026-07-29: Suggested changes batch implemented local OAuth origin verification, configurable job workflow permissions, and optional local email/password login.
- 2026-07-29: Request-access flow implemented and click-through verified.
- 2026-07-29: Existing-user request handling refined for reactivation/existing-account admin review.
- 2026-07-29: Jobs click-test fixes implemented: Cancelled under Closed, fixed status control sizing, status-change notes, and status audit history with admin-editable notes.
- 2026-07-30: Local email/password sign-in edge cases manually tested by the user and accepted for now.
- 2026-07-30: Trustee-only job access and assignment implemented: removed roles and granular job permissions, added `IsPortalAdmin`, backfilled existing admins via `TrusteeOnlyPortalAdmins`, added round-robin job assignment, added job editing, enforced portal-admin-or-assigned-trustee mutation rules, and simplified user/access UIs. Verified with local migration, backend build, 22/22 domain tests, 47/47 application tests, frontend build, and frontend lint. Existing frontend Fast Refresh warning in `AuthContext.tsx` remains.
- 2026-07-30: Jobs row UX refined: inline history/edit actions were moved into a larger job detail modal opened from the job title. The table keeps only quick status changes for authorized users and portal-admin assignment in the assigned-to column; the modal contains read-only job details, edit mode for authorized users, and a scrollable notes/history section. Verified with frontend build and lint.
