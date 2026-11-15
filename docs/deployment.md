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

- pushes the API image to GHCR as `:<version>` and `:latest`;
- triggers the Render deploys through the `RENDER_DEPLOY_HOOK_URL` (API) and
  `RENDER_FRONTEND_DEPLOY_HOOK_URL` (static site) secrets, each skipped with a notice if unset;
- builds the coverage badges (ReportGenerator) and the demo GIF (`e2e/ContactHub.Demo`, ffmpeg), and
  pushes them to the orphan `badges` branch from a single job.

Coverage is reported per suite, one pair of badges (line, branch) each at
`badges/coverage/<app>/<suite>/{line,branch}.svg` on the `badges` branch: backend unit
(`ContactHub.Api.Tests`, coverlet.msbuild), backend integration (`ContactHub.Api.Integration.Tests`,
`--collect "XPlat Code Coverage"`), backend e2e, frontend unit (`npm run test:coverage`) and frontend e2e.
Backend reports are filtered to `+ContactHub.*;-ContactHub.*Tests*;-ContactHub.E2E*`.

The e2e job sets `E2E_COVERAGE=true`. The backend then runs under `dotnet-coverage collect` (installed
as a global tool; Cobertura at `e2e/artifacts/coverage/backend/`, written when the suite shuts the
session down), and every Playwright page records V8 coverage over CDP into
`e2e/artifacts/coverage/frontend/raw/`. `npm run coverage:e2e` converts that to
`cobertura-coverage.xml`. Both are uploaded as the `e2e-coverage` artifact for the `coverage` job.
Local runs without the variable are unchanged.

## Hosted on Render

The frontend and the API are hosted separately. `render.yaml` is a Blueprint with two services,
both with `autoDeploy: false`, so a failing commit never goes live:

| Service | Type | URL | What it serves |
| --- | --- | --- | --- |
| `contacthub-web` | Static site | <https://contacthub-web.onrender.com> | The built Angular app (`frontend/dist/frontend/browser`) |
| `contacthub` | Docker web service (free, Frankfurt) | <https://contacthub-h4or.onrender.com> | API and Swagger only, from the root `Dockerfile` on port 8080 |

The static site has two rewrite rules, in order:

1. `/api/*` → `https://contacthub-h4or.onrender.com/api/*`. The browser stays on one origin, so there is no CORS and the
   Angular app keeps calling the relative `/api`, exactly as with the dev server proxy.
2. `/*` → `/index.html`, so deep links such as `/contacts/123` load the Angular router.

The API service sets `SeedDemoData=true`, `healthCheckPath: /api/health` and
`ClientUrl=https://contacthub-web.onrender.com`, so Swagger's **Client** button opens the static site.

To set it up in another account:

1. In Render choose New, Blueprint, and select this repository. Render reads `render.yaml` and
   creates both services. If Render assigns different URLs, update the `/api/*` rewrite and
   `ClientUrl` in `render.yaml`.
2. For each service open Settings, Deploy Hook, and copy the URL.
3. In GitHub, Settings, Secrets and variables, Actions, add `RENDER_DEPLOY_HOOK_URL` (API) and
   `RENDER_FRONTEND_DEPLOY_HOOK_URL` (static site).
4. Every green push to `main` now triggers both deploys. A missing secret skips its step.

## Tests, coverage and demo

- **Backend:** `ContactHub.Api.Tests` holds unit tests in folders per layer (`Domain/`,
  `Application/`, `Infrastructure/`, `Api/`, `Architecture/`) and fails below 100% line or branch
  coverage (coverlet). `ContactHub.Api.Integration.Tests` runs the API in memory through
  `WebApplicationFactory`.
- **Frontend:** Vitest covers the reducer, selectors, effects, API service, validators and
  components. `npm run test:coverage` also writes `coverage/frontend/cobertura-coverage.xml`.
- **E2E:** `e2e/ContactHub.E2E` is an NUnit test project using Microsoft.Playwright (Chromium), run
  with `dotnet test e2e/ContactHub.E2E`. It covers every user flow (41 flows, 50 test cases, listed
  in [QA](qa.md#9-playwright-e2e-test)). A `[SetUpFixture]` starts the backend and the dev server
  once; on failure the test saves a screenshot and a Playwright trace to `e2e/artifacts/` and
  attaches them to the result. Install the browser once after a build with
  `pwsh e2e/ContactHub.E2E/bin/Debug/net10.0/playwright.ps1 install chromium`. Lint it with
  `cd e2e && dotnet format --verify-no-changes`.
- **Demo:** `e2e/ContactHub.Demo` is a separate Playwright recorder with no assertions. It starts
  the stack with demo contacts and records Swagger's Client button, search, copy, create, edit and
  delete to `e2e/artifacts/demo/demo.webm` (about 70 s, 1 s steps, visible typing and a click
  marker).
