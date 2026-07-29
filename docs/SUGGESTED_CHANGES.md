# Suggested Changes Log

Last updated: 2026-07-29

Use this as the running list for click-through notes, UX polish, bugs, and feature ideas. Keep entries small and actionable so we can promote them into implementation work when ready.

## Intake

| Status | Area | Suggestion | Notes |
| --- | --- | --- | --- |
| Done | Login | Use `http://localhost:5173` for local OAuth testing, not `http://127.0.0.1:5173`. | Verified 2026-07-29: dev CORS allows `http://localhost:5173`; keep using that origin for Google OAuth. |
| Done | Authorization | Revisit the user permission model for job workflows. | Implemented 2026-07-29: users now have configurable job permissions for loading, creating, updating status, and assigning jobs. |
| Done | Login/Auth | Add a local email-and-password login option alongside Google sign-in. | Implemented 2026-07-29: optional per-user local password hash plus `/api/auth/password` and frontend email/password sign-in. |
| Done | Login/Auth | Add a "Don't have an account? Request access" flow. | Implemented and user click-through verified 2026-07-29: anonymous users can submit details for admin review; approval creates/enables a real user. |
| Done | Access Requests | Route existing-user access attempts to admins instead of blocking or duplicating users. | Implemented and user click-through verified 2026-07-29: access requests link to existing users and show as reactivation/existing-account cards in the Access tab. |

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
