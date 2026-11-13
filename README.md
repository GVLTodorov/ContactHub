# <img src="frontend/public/favicon.svg" alt="" width="40" align="top"> ContactHub

A small, single-user contact manager built as an interview task
([requirements](docs/requirements.md), [design plan](docs/plan.md)): an ASP.NET Core (.NET 10) Web
API with SQLite and an Angular UI. It is hosted on [Render](https://render.com) and redeploys from
`main` after CI passes.

| What | URL |
| --- | --- |
| App (list, search, create, edit, delete contacts) | <https://contacthub-h4or.onrender.com> |
| Swagger UI (API docs, try it out) | <https://contacthub-h4or.onrender.com/swagger> |
| Health check | <https://contacthub-h4or.onrender.com/api/health> |
| Source | <https://github.com/GVLTodorov/ContactHub> |

> The free tier sleeps after about 15 minutes without traffic, so the first visit can take up to a
> minute to load. Data resets to five demo contacts on every restart or deploy.

| Project | Line coverage | Branch coverage |
| --- | --- | --- |
| Backend | ![Backend line coverage](https://github.com/GVLTodorov/ContactHub/raw/badges/coverage/backend/line.svg) | ![Backend branch coverage](https://github.com/GVLTodorov/ContactHub/raw/badges/coverage/backend/branch.svg) |
| Frontend | ![Frontend line coverage](https://github.com/GVLTodorov/ContactHub/raw/badges/coverage/frontend/line.svg) | ![Frontend branch coverage](https://github.com/GVLTodorov/ContactHub/raw/badges/coverage/frontend/branch.svg) |

![Demo: Swagger UI's Client button opens the app, then the seeded list is searched, a contact's phone and address are copied, a new contact is created (an invalid IBAN is fixed), edited and deleted.](https://github.com/GVLTodorov/ContactHub/raw/badges/demo/demo.gif)

## Features

- Each contact has a **first name**, **surname**, **date of birth**, **address** (street, postal
  code, city, country), **phone number** and **IBAN**.
- List, view, create, edit and delete contacts, stored in SQLite. No accounts.
- One search box filters by name, phone, IBAN or city, plus column sorting and paging.
- Validation in the browser, on each request and in the domain model (IBAN length per country and mod-97 checksum, phone
  format, date of birth range). Server errors show under the matching field.
- Responsive: on narrow screens the table collapses to stacked rows.

**Stack:** ASP.NET Core Web API (controllers, EF Core + SQLite, FluentValidation, hand-rolled CQRS,
Clean Architecture) and Angular (ngrx, PrimeNG, Tailwind CSS v4), with NUnit, Vitest and Playwright
tests, Docker Compose and GitHub Actions.

## How to run

**Prerequisites:** .NET SDK 10 (`global.json`) and Node 24 (`.nvmrc`). Docker only for Compose.

| What | Command | Result |
| --- | --- | --- |
| Backend | `cd backend && dotnet run --project ContactHub.Api` | API on http://127.0.0.1:8080, Swagger at `/swagger` (its **Client** button opens the app) |
| Frontend | `cd frontend && npm install && npm run start` | App on http://127.0.0.1:4700, `/api` proxied to the backend |
| Whole stack | `docker compose up --build` | Backend on 8080, frontend on 4700 |
| Backend tests | `cd backend && dotnet test` | NUnit unit tests (95% coverage gate) and integration tests |
| Frontend tests | `cd frontend && npm test -- --watch=false` | Vitest |
| E2E test | `dotnet run --project e2e/ContactHub.E2E` (repo root) | Playwright; starts the stack itself (first time: add `-- install chromium`) |
| Demo video | `dotnet run --project e2e/ContactHub.Demo` (repo root) | Records `e2e/artifacts/demo/demo.webm` |
| Lint | `cd backend && dotnet format --verify-no-changes` · `cd frontend && npm run lint` | |

The local database is created on first run in the app's output folder. See
[Deployment, CI and data](docs/deployment.md) for where it lives, the CI pipeline, tests and Render setup.

## Architecture

The backend follows Clean Architecture, one project per layer, with references pointing inward only:

```text
ContactHub.Api → ContactHub.Infrastructure → ContactHub.Application → ContactHub.Domain
```

- **Domain:** the `Contact` aggregate with private setters and the value objects `Address`,
  `PhoneNumber` and `Iban`. Invalid state can't be constructed.
- **Application:** commands and queries with their handlers and request validators. Handlers use
  `IAppDbContext`, never Infrastructure.
- **Infrastructure:** `AppDbContext`, EF configurations, migrations and the demo data seeder.
- **Api:** thin controllers and exception handlers that map errors to `400`/`404` problem details.

Architecture tests (NetArchTest) fail the build if a layer references the wrong one. The frontend
uses an ngrx store (actions → effects → API → reducer → selectors read as signals).

Full details, diagrams and validation rules: [Architecture](docs/architecture.md).

## Requirements to code

A full checklist with line links and how to verify each item: [QA](docs/qa.md).

| Requirement | Where |
| --- | --- |
| Data persisted | `backend/ContactHub.Infrastructure/DependencyInjection.cs` (SQLite path), `backend/ContactHub.Api/Startup.cs` (`Database.Migrate()`), `compose.yml` (`dbdata` volume) |
| EF Core | `backend/ContactHub.Infrastructure/Persistence/` (`AppDbContext.cs`, `Configurations/`, `Migrations/`) |
| FluentValidation (models) | `backend/ContactHub.Application/Contacts/Commands/*/…Validator.cs`, `backend/ContactHub.Application/Behaviors/ValidationCommandHandlerDecorator.cs` |
| FluentValidation (entities) | `backend/ContactHub.Domain/Entities/Contact.cs` (`ContactValidator`), `backend/ContactHub.Domain/ValueObjects/Address.cs` (`AddressValidator`) |
| CQRS | `backend/ContactHub.Application/Abstractions/`, `backend/ContactHub.Application/Contacts/`, `backend/ContactHub.Api/Controllers/ContactsController.cs` |
| Rich Domain Model | `backend/ContactHub.Domain/` (`Entities/Contact.cs`, `ValueObjects/`, `Exceptions/`) |
| Clean Architecture | `backend/ContactHub.*` projects, `backend/ContactHub.Api.Tests/Architecture/ArchitectureTests.cs` |
| ngrx Store | `frontend/src/app/store/contacts/`, `frontend/src/app/app.config.ts`, `frontend/src/app/api/contacts-api.ts` |
| PrimeNG components | `frontend/src/app/app.config.ts` (`providePrimeNG`, Aura), `frontend/src/app/pages/contacts/contacts.html`, `frontend/src/app/components/contact-form/contact-form.html`, `frontend/src/app/components/layout/layout.html` |
| Playwright e2e test | `e2e/ContactHub.E2E/`, `e2e` job in `.github/workflows/ci.yml` |
| Tests | `backend/ContactHub.Api.Tests/` (unit, by layer), `backend/ContactHub.Api.Integration.Tests/` (HTTP endpoints), `*.spec.ts` next to the frontend code |
