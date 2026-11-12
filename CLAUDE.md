# Project: ContactHub

## Stack

- .NET SDK 10 (`global.json`), nullable C#; Node 24 (`.nvmrc`)
- `backend/`: ASP.NET Core Web API with controllers (no Minimal APIs), `src/Api/`, NUnit in `tests/Api.Tests/`, one solution file
- `frontend/`: tested with Vitest
- `Dockerfile`, `compose.yml`, `.github/`: Docker Compose, GitHub Actions

## Commands

- From `backend/`: dev `dotnet run --project src/Api` · lint `dotnet format --verify-no-changes`
  · test `dotnet test` · build `dotnet build`
- Whole stack: `docker compose up --build`

## Frontend

- Framework: Angular (latest), TypeScript, Tailwind CSS (latest, v4+)
- Commands: `cd frontend && npm run start | lint | test | build`; quiet tests
  `npm test -- --watch=false`
- Dev server: `http://127.0.0.1:4200`; in Docker `npx ng serve --host 0.0.0.0`
- VS Code launch: `frontend/node_modules/@angular/cli/bin/ng.js serve --host 127.0.0.1`
- Build output: `frontend/dist/frontend/browser/`
- Local dev values: `src/environments/`
- VS Code extensions: `angular.ng-template`

## Database

- Database: SQLite via EF Core (`Microsoft.EntityFrameworkCore.Sqlite`, `UseSqlite`)
- Data code: entities and `AppDbContext` in `src/Api/Data/`. SQLite can't sort `DateTimeOffset`
  or `decimal`: use UTC `DateTime` and integer cents
- Schema changes, from `backend/`: `dotnet ef migrations add <Change> --project src/Api` (first
  time `dotnet tool restore`). The app applies them at startup with `Database.Migrate()`
- Database file: `database/contacthub.db` inside the running app's folder, e.g.
  `backend/src/Api/bin/Debug/net10.0/database/contacthub.db`, created on first run. A relative
  `Data Source` is resolved against `AppContext.BaseDirectory`, never the working directory or
  content root; startup creates the file's folder before `Database.Migrate()`. `*.db*` files are
  gitignored
- Local `ConnectionStrings:Default`: `Data Source=database/contacthub.db`
- Tests: `Data Source=database/contacthub_test.db` in `appsettings.Testing.json` (lands in the test
  project's output folder); one `[OneTimeSetUp]` deletes the file before the factory starts,
  startup recreates it
- Compose: no `db` service; backend mounts a named volume on `/app/database`, same
  `Data Source=database/contacthub.db`
- Image: final stage creates `/app/database` owned by `app`
- CI: no database service

## Rules

- Edit only your area. Need another? Say so in your report.
- Run your area's lint, build and tests yourself before reporting; after a fix, rerun only what
  failed. Max 2 fix rounds, then report the failure.
- Only the brain commits, once per `/task`. Nobody pushes.
- Commit local dev values (`appsettings.Development.json`, the Frontend section's place), never
  real secrets.
- `127.0.0.1`, not `localhost`. Backend dev: `http://127.0.0.1:8080`; frontend proxies `/api`.
- Stop every server or container you started before reporting.
- Command output: quiet flags (`--nologo -v q` for `dotnet`, the Frontend section's quiet tests),
  last 50 lines only.
- Never read `bin`, `obj`, `TestResults`, `node_modules`, `.angular`, `dist`, `coverage`, lockfiles.
- Reports: failing lines only, never whole files or logs.
