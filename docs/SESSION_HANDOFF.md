# Handoff: continuing this project in a new (local) Claude Code session

This file exists so a fresh Claude Code session — e.g. running locally on your
machine instead of the cloud sandbox — can pick up exactly where the previous
session left off, without re-deriving context. Once you no longer need it (e.g.
after Phase 2 is well underway), feel free to delete this file; it's a handoff
aid, not a permanent project doc. `docs/ARCHITECTURE.md` and
`docs/MANUAL_TEST_PLAN.md` are the permanent ones.

## Where things stand

**Phase 1 (Authentication + Users) is fully implemented, tested, and pushed to
`main`** — 16 commits, working tree clean at time of writing. Both backend
(`dotnet build && dotnet test`) and frontend (`npm run build && npm run lint`)
are green.

What's built: Clean Architecture .NET backend (`Bcmp.Domain` / `Application` /
`Infrastructure` / `Api`) with EF Core + Postgres, Google ID token → app-issued
JWT sign-in gated by the Users table (no self-registration), full Users CRUD
with `RequireAdministrator`/`RequireTrustee` policies, an idempotent
`--seed` bootstrap-admin command, Serilog + global exception handling, CORS.
React + TypeScript frontend (Vite) with Google sign-in, an in-memory-JWT auth
context, route guards, and an admin Users screen.

**Read `docs/ARCHITECTURE.md` first** — it records every decision with a real
trade-off (bearer JWT vs cookie, explicit migrations, the plain `"role"` claim
type, CORS) including four bugs that were only found by actually running the
app against live Postgres/API/browser, not by code review alone. Worth
internalizing before touching this code so you don't reintroduce them.

## What's NOT done yet

**Google OAuth click-testing, partially done.** A real human has now signed
in through the actual Google consent screen as the bootstrap admin and
confirmed it works end-to-end (step 1 of the checklist in
`docs/MANUAL_TEST_PLAN.md` §"What still needs a human, with a real Google
account", verified 2026-07-20). Steps 2–5 of that checklist — unknown-account
rejection, adding a second user who then signs in, a disabled user being
rejected on next login, and the Trustee route guard — are still outstanding;
they need a second real Google test-user account to exercise. Worth picking
up next if you want full confidence before moving on to Phase 2.

Also since the last handoff: a small UI polish pass landed on the login page
(avoids an app-shell flash before redirecting, bounces `/login` to the
intended page if already signed in, drops the hero image's dark overlay) —
see the "Polish login page" commit.

**Phase 2's Jobs domain is now implemented, plus a first feedback round**
(see the "Add Jobs domain", "Add Jobs list screen", "Track
Job.UpdatedAtUtc", "Add sortable columns...", and "Add assignable trustee
field..." commits) — see `docs/PROJECT_STATE.md` for the full picture, but
the short version: `Job` entity with a `JobSource` parameter on creation so
a future email-ingestion path is a new caller, not a refactor; sortable
columns on every field; an Active/Completed split (Completed = its own
section at the bottom, everything else counts as Active including
Cancelled — that grouping was my call, not explicitly specified, flag it if
it should be different); and a nullable `AssignedTrusteeUserId` with an
Administrator-only `PATCH /api/jobs/{id}/assign` endpoint as groundwork for
the future Assignment engine phase (routing/notifications still not
built — this is just "an admin can point a job at a trustee").

Every backend increment was live-verified against Postgres/API (curl + a
hand-crafted JWT for both Administrator and Trustee roles); 42/42 unit
tests green. `npm run build`/`npm run lint` are green throughout, which
only proves it compiles, not that it works — **the frontend has not been
click-tested by Claude in a real browser** (no headless-browser tooling in
this local session), though the user was actively driving the running dev
instance while giving feedback, so the core flows are very likely fine.
Worth an explicit pass through `/jobs` (add, sort, complete, assign) before
treating it as done.

**Phase 2+ (Properties, Jobs, Email integration, Assignment engine,
Dashboards, AI enrichment)**: not started. See `docs/ARCHITECTURE.md`'s
"Roadmap" section (carried over from the original plan) for the intended
shape of each — Jobs in particular should go behind a pluggable `IJobSource`
abstraction from day one so email-based creation slots in later without
refactoring.

## Getting it running locally

Prerequisites: .NET 10 SDK, Node.js 20+, PostgreSQL running locally (or any
reachable Postgres instance).

```bash
git clone https://github.com/matthew-turnbull-qa/body-corporate-portal.git
cd body-corporate-portal
```

### Backend

```bash
cd backend
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=bcmp_dev;Username=postgres;Password=<your-postgres-password>" --project src/Bcmp.Api
dotnet user-secrets set "Auth:Google:ClientId" "957944850245-2or723i0fsccs6id4jlrugj9158ic44l.apps.googleusercontent.com" --project src/Bcmp.Api
dotnet user-secrets set "Auth:Jwt:SigningKey" "$(openssl rand -base64 48)" --project src/Bcmp.Api
dotnet user-secrets set "Bootstrap:AdminEmail" "beavispta@gmail.com" --project src/Bcmp.Api
dotnet ef database update --project src/Bcmp.Infrastructure --startup-project src/Bcmp.Api
dotnet run --project src/Bcmp.Api -- --seed
dotnet run --project src/Bcmp.Api
```

Runs on `http://localhost:5151` (matches `launchSettings.json`).

### Frontend (separate terminal)

```bash
cd frontend
cp .env.example .env
# edit .env: set VITE_GOOGLE_CLIENT_ID=957944850245-2or723i0fsccs6id4jlrugj9158ic44l.apps.googleusercontent.com
npm install
npm run dev
```

Runs on `http://localhost:5173` — this exact origin is already configured as
an authorized JavaScript origin on the Google OAuth client above, and is
already allowed by the backend's dev CORS config
(`backend/src/Bcmp.Api/appsettings.Development.json`). Don't change the port
without updating both.

## Google Cloud OAuth setup (already done, for reference)

A Google Cloud project and OAuth client already exist (Client ID above,
"Testing" publish status). If you need to add more test-user emails (required
while the app is in Testing status — only listed emails can sign in): Google
Cloud Console → **APIs & Services → Google Auth Platform → Audience** tab →
**Test users → + Add users**. `beavispta@gmail.com` (the bootstrap admin) is
already added.

## Working conventions established so far

- **Feature-by-feature, not all at once.** Each feature got a design
  explanation + rationale + trade-offs *before* implementation, per the
  original project brief. Keep doing this for Phase 2+.
- **Verify live, not just unit tests.** Every backend feature was exercised
  against real Postgres via `dotnet run` + `curl`/hand-crafted JWTs before
  being called done; every frontend feature was checked with a headless
  browser against the real running backend. This caught bugs unit tests
  didn't (see `docs/ARCHITECTURE.md`). Worth keeping up for Phase 2.
- **Commit granularity**: one commit per numbered implementation step, each
  buildable/testable on its own — not one giant commit per feature.
- **Cost-consciousness**: every infra choice (Neon Postgres, Render backend,
  Vercel/Netlify frontend) was picked for free-tier viability; call out cost
  implications explicitly for any new infrastructure choice in Phase 2+.
