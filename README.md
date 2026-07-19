# Body Corporate Management Portal

A portal for trustees of a residential complex to manage maintenance requests, resident queries, and operational work. **Jobs** are the primary entity — email is just one of several ways a Job gets created.

## Structure

- `backend/` — ASP.NET Core (.NET 10) API, Clean Architecture (`Bcmp.Domain` / `Bcmp.Application` / `Bcmp.Infrastructure` / `Bcmp.Api`).
- `frontend/` — React + TypeScript SPA (Vite).
- `docs/` — architecture decisions and manual test plan.

See `docs/ARCHITECTURE.md` for key design decisions and their trade-offs.

## Local development

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
