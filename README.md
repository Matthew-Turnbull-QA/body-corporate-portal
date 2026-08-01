# Body Corporate Management Portal

A portal for trustees of a residential complex to manage maintenance requests, resident queries, and operational work. **Jobs** are the primary entity — email is just one of several ways a Job gets created.

## Structure

- `backend/` — ASP.NET Core (.NET 10) API, Clean Architecture (`Bcmp.Domain` / `Bcmp.Application` / `Bcmp.Infrastructure` / `Bcmp.Api`).
- `frontend/` — React + TypeScript SPA (Vite).
- `docs/` — architecture decisions and manual test plan.

See `docs/ARCHITECTURE.md` for key design decisions and their trade-offs.

## Local development

### Start the app

On Windows, after local secrets and `frontend/.env` are configured:

```powershell
npm run dev:all
```

This opens separate PowerShell windows for:

- Backend API: `http://localhost:5151`
- Frontend app: `http://localhost:5173`

Optional first-run switches:

```powershell
.\scripts\start-dev.ps1 -Install -Migrate -Seed -OpenBrowser
```

### Backend

```bash
cd backend
dotnet user-secrets set "ConnectionStrings:Default" "<postgres-connection-string>" --project src/Bcmp.Api
dotnet user-secrets set "Auth:Google:ClientId" "<google-oauth-client-id>" --project src/Bcmp.Api
dotnet user-secrets set "Auth:Jwt:SigningKey" "<random-secret>" --project src/Bcmp.Api
dotnet user-secrets set "Bootstrap:AdminEmail" "<your-google-email>" --project src/Bcmp.Api
dotnet ef database update --project src/Bcmp.Infrastructure --startup-project src/Bcmp.Api
dotnet run --project src/Bcmp.Api -- --seed   # one-time: creates the first Administrator, safe to re-run
dotnet run --project src/Bcmp.Api
```

Optional Gmail email intake for local POC:

```bash
cd backend
dotnet user-secrets set "EmailIntake:Enabled" "true" --project src/Bcmp.Api
dotnet user-secrets set "EmailIntake:SystemUserEmail" "email-intake@system.local" --project src/Bcmp.Api
dotnet user-secrets set "EmailIntake:FolderName" "INBOX" --project src/Bcmp.Api
dotnet user-secrets set "EmailIntake:PollIntervalSeconds" "300" --project src/Bcmp.Api
dotnet user-secrets set "EmailIntake:MaxMessagesPerPoll" "10" --project src/Bcmp.Api
dotnet user-secrets set "EmailIntake:Gmail:Address" "<rietvlei-gmail-address>" --project src/Bcmp.Api
dotnet user-secrets set "EmailIntake:Gmail:ClientId" "<google-oauth-client-id>" --project src/Bcmp.Api
dotnet user-secrets set "EmailIntake:Gmail:ClientSecret" "<google-oauth-client-secret>" --project src/Bcmp.Api
dotnet run --project src/Bcmp.Api -- --seed-email-intake
dotnet run --project src/Bcmp.Api -- --gmail-oauth
```

The `--gmail-oauth` command opens Google consent for the configured Gmail account and prints a `dotnet user-secrets set "EmailIntake:Gmail:RefreshToken" ...` command. Run the printed command, then restart the API.

If local antivirus/VPN/proxy TLS inspection causes Gmail certificate validation to fail during local POC testing, this can temporarily unblock development:

```bash
dotnet user-secrets set "EmailIntake:Gmail:AllowInvalidServerCertificate" "true" --project src/Bcmp.Api
```

Do not use that setting in production; fix the machine/root-certificate/network issue instead.

Email-created jobs start as `Open` with no unit selected. A trustee/admin must select a unit before the status can change. The app replies to the sender with the job number and BCCs enabled trustees.

### Frontend

```bash
cd frontend
cp .env.example .env
npm install
npm run dev
```

## Testing

```bash
cd backend && dotnet test
cd frontend && npm test
```

## Hosting (free-tier)

- Database: [Neon](https://neon.tech) free Postgres.
- Backend API: [Render](https://render.com) free web service.
- Frontend: Vercel or Netlify free static hosting.

Free-tier backend/DB instances spin down when idle — the first request after a period of inactivity will be slow (cold start).
