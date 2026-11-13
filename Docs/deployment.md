# Deployment, CI and data

How ContactHub is built, tested, hosted and where it keeps its data. For how to run it
locally, see the [README](../README.md).

## Where the data lives

The connection string is `Data Source=database/contacthub.db`. A
relative path is resolved against the running app's folder (`AppContext.BaseDirectory`), not the
working directory. Locally the file is
`backend/ContactHub.Api/bin/Debug/net10.0/database/contacthub.db`. Startup creates the folder and runs
`Database.Migrate()`, so the database is created on first run. Tests use `contacthub_test.db` in
the test project's output folder, and the e2e run uses `contacthub_e2e.db`, which is deleted
before each run. In Docker Compose the backend mounts the named volume `dbdata` on
`/app/database`. Running `dotnet clean` or deleting `bin/` wipes local data.

## CI pipeline

CI (`.github/workflows/ci.yml`) is one pipeline. On every push and pull request it runs the backend
(format, build, unit and integration tests with coverage), frontend (lint, tests with coverage,
build) and e2e jobs. On `main`, once those pass, it then:

- pushes the single image (API plus the built Angular app) to GHCR as `:<version>` and `:latest`;
- triggers a Render deploy through the `RENDER_DEPLOY_HOOK_URL` secret (skipped with a notice if unset);
- builds the coverage badges (ReportGenerator) and the demo GIF (`e2e/ContactHub.Demo`, ffmpeg), and
  pushes them to the orphan `badges` branch from a single job.

Backend coverage combines `ContactHub.Api.Tests` (coverlet.msbuild) and `ContactHub.Api.Integration.Tests`
(`--collect "XPlat Code Coverage"`). Frontend coverage is reported separately
(`npm run test:coverage`).

## Hosted on Render

The app is live at <https://contacthub-h4or.onrender.com>. `render.yaml` describes the free Docker
web service `contacthub` (Frankfurt) built from the root `Dockerfile` (final stage `app`: API,
Swagger and the SPA on port 8080). It sets `SeedDemoData=true`, `healthCheckPath: /api/health` and
`autoDeploy: false`, so a failing commit never goes live. To set it up in another account:

1. In Render choose New, Blueprint, and select this repository. Render reads `render.yaml`.
2. Open the service, Settings, Deploy Hook, and copy the URL.
3. In GitHub, Settings, Secrets and variables, Actions, add the secret `RENDER_DEPLOY_HOOK_URL`.
4. Every green push to `main` now triggers a deploy. Without the secret the step is skipped.

## Tests, coverage and demo

- **Backend:** `ContactHub.Api.Tests` holds unit tests in folders per layer (`Domain/`,
  `Application/`, `Infrastructure/`, `Api/`, `Architecture/`) and fails below 95% line or branch
  coverage (coverlet). `ContactHub.Api.Integration.Tests` runs the API in memory through
  `WebApplicationFactory`.
- **Frontend:** Vitest covers the reducer, selectors, effects, API service, validators and
  components. `npm run test:coverage` also writes `coverage/frontend/cobertura-coverage.xml`.
- **E2E:** `e2e/ContactHub.E2E` is a .NET console app using Microsoft.Playwright (Chromium). It
  starts the backend and the dev server itself and exits non-zero on failure. Lint it with
  `cd e2e && dotnet format --verify-no-changes`.
- **Demo:** `e2e/ContactHub.Demo` is a separate Playwright recorder with no assertions. It starts
  the stack with demo contacts and records Swagger's Client button, search, copy, create, edit and
  delete to `e2e/artifacts/demo/demo.webm` (about 70 s, 1 s steps, visible typing and a click
  marker).
