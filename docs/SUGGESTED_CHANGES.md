# Suggested Changes Log

Last updated: 2026-07-31

Use this as the running list for click-through notes, UX polish, bugs, and feature ideas. Keep entries small and actionable so we can promote them into implementation work when ready.

## Intake

| Status | Area | Suggestion | Notes |
| --- | --- | --- | --- |
| Done | Login | Use `http://localhost:5173` for local OAuth testing, not `http://127.0.0.1:5173`. | Verified 2026-07-29: dev CORS allows `http://localhost:5173`; keep using that origin for Google OAuth. |
| Done | Authorization | Revisit the user permission model for job workflows. | Implemented 2026-07-29: users now have configurable job permissions for loading, creating, updating status, and assigning jobs. |
| Done | Login/Auth | Add a local email-and-password login option alongside Google sign-in. | Implemented 2026-07-29: optional per-user local password hash plus `/api/auth/password` and frontend email/password sign-in. |
| Done | Login/Auth | Add a "Don't have an account? Request access" flow. | Implemented and user click-through verified 2026-07-29: anonymous users can submit details for admin review; approval creates/enables a real user. |
| Done | Access Requests | Route existing-user access attempts to admins instead of blocking or duplicating users. | Implemented and user click-through verified 2026-07-29: access requests link to existing users and show as reactivation/existing-account cards in the Access tab. |
| Done | Jobs | Prevent status text length from resizing rows. | Implemented 2026-07-29: status chips and status selects now use fixed dimensions; Chrome click-through measured consistent sizes. |
| Done | Jobs | Treat Cancelled as closed rather than active. | Implemented 2026-07-29: Jobs page now groups `Completed` and `Cancelled` under Closed. |
| Done | Jobs | Ask for optional notes when changing status. | Implemented 2026-07-29: status select opens a confirmation modal with optional notes before saving. |
| Done | Jobs | Track status-change audit history and allow admin note edits. | Implemented 2026-07-29: `JobStatusHistory` records transitions, all users with `LoadJobs` can view history, and Administrators can edit notes while immutable audit fields remain unchanged. |
| New | Users | Show whether each user has a local password set. | Add a read-only field/tag to the Users screen so portal admins can tell whether password sign-in is configured for a user. |
| New | Access Requests | Keep linked access-request user details in sync after user edits. | If a portal admin updates a user's name, the corresponding access request page still shows the old name, which can cause confusion. |
| Deferred | Email Intake | Harden inbound email against junk once the dedicated Gmail address becomes public. | The Rietvlei intake mailbox will eventually reach marketers/spam lists; POC should rely on Gmail spam handling plus a dedicated label, then later add allowlists, review queues, or stronger sender validation. |
| Deferred | Email Notifications | Email the requester when notes are added to their job. | Scope later: decide which note types should trigger requester updates, whether internal-only notes are needed, and how replies should be worded. |

## Status Key

- New: captured, not yet triaged.
- Accepted: agreed for implementation.
- In progress: currently being worked on.
- Done: implemented and verified.
- Deferred: useful, but intentionally parked.
- Rejected: decided against, with reason in notes.

## Review Rhythm

- Add click-through notes here as they come up.
- At the end of a feedback pass, group related items and pick the next implementation batch.
- When an item is completed, move its status to `Done` and add the verification note or date.
