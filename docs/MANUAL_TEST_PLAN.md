# Manual Test Plan — Phase 1 (Auth + Users)

## Already verified during development (automated / live, not just unit tests)

Every item below was exercised against a real local Postgres instance and a running
instance of the API (and, where noted, a running instance of the frontend), not just
asserted in unit tests. Three real bugs were found and fixed this way (JWT role-claim
mapping, `UserRole` enum JSON (de)serialization, and an EF Core change-tracking
conflict on update) — see the git history for details.

- `dotnet build` / `dotnet test` green across all backend projects (35 unit tests: Domain, Application).
- `npm run build` / `npm run lint` green on the frontend.
- `InitialCreate` migration applies cleanly to Postgres and produces the expected `Users` table/index shape.
- `POST /api/auth/google` with an invalid token → `401`.
- `GET /api/users/me` with no token → `401`; with a valid token for an enabled user → `200` with correct data.
- `GET /api/users` (list) as Administrator → `200`; as Trustee → `403`.
- `POST /api/users` with a new email → `201`; with a duplicate email → `409`.
- `PATCH /api/users/{id}/disable` on the last enabled Administrator → `409` (guard holds).
- `PATCH /api/users/{id}/disable` / `.../enable` / `PUT /api/users/{id}` on a non-last-admin user → succeed, table reflects the change.
- `--seed` creates the bootstrap Administrator from `Bootstrap:AdminEmail`; running it again is a no-op; normal `dotnet run` works fine with `Bootstrap:AdminEmail` completely unset.
- Frontend: visiting `/` while signed out redirects to `/login` and renders the Google sign-in button with no app errors.
- Frontend: with an authenticated session, the Users page lists users, and Add / Edit / Enable / Disable all work end-to-end against the live API, including the UI correctly disabling the "Disable" button on your own account.
- CORS: cross-origin requests from the Vite dev origin to the API succeed once `Cors:AllowedOrigins` includes that origin (this was broken and fixed during development).

## What still needs a human, with a real Google account

The one thing that cannot be verified from an automated/headless environment is the
actual Google consent screen — it requires a live Google Cloud OAuth Client ID and a
real Google account clicking through Google's own UI. Once you have a Client ID (see
README for where to set it), run through this checklist yourself:

1. **Bootstrap admin can sign in.** — ✅ **PASS (2026-07-20)**
   Sign in with the Google account matching `Bootstrap:AdminEmail`. Expect: redirected
   to the home page, header shows your name and "(Administrator)", `/users` link is visible.

2. **Unknown Google account is rejected.**
   Sign in with a Google account that has never been added as a user. Expect: sign-in
   fails with "This Google account is not registered, or has been disabled. Contact
   your administrator." — no new user is created (check the Users table / admin list).

3. **Admin adds a second user, who can then sign in.**
   As the admin, use "Add user" to add a second Google account's email as a Trustee.
   Sign out, sign in with that second Google account. Expect: success, header shows
   "(Trustee)", no "Users" link visible (Trustee can't reach admin screens).

4. **Disabled user is rejected on next login.**
   As the admin, disable the Trustee from step 3. Have that Trustee try to sign in
   again (or refresh if still signed in and hit any API call). Expect: rejected the
   same way as step 2 — a disabled user gets the same "not registered or disabled"
   message the API returns for an unknown one, by design (§3.1 of the architecture:
   no distinction is leaked between "unknown" and "disabled").

5. **Trustee is blocked from admin routes, not just hidden UI.**
   While signed in as the Trustee, manually navigate the browser to `/users`. Expect:
   redirected away (client-side `RequireRole` guard). This is defense-in-depth, not the
   real boundary — the actual enforcement is server-side (`RequireAdministrator` policy
   on every admin endpoint), which was already verified with a Trustee-role JWT getting
   `403` from the API directly.

Steps 2–5 (unknown-account rejection, second-user add + sign-in, disabled-user
rejection, Trustee route guard) are still outstanding — need a second real
Google test-user account to exercise.

Record the outcome of each step here (pass/fail + date) once you've run through it.
