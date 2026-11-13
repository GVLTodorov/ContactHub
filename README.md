# ContactHub

A single-user contact manager: an ASP.NET Core (.NET 10) Web API with SQLite and an Angular UI. It
is hosted on [Render](https://render.com) (free tier, Frankfurt) and redeploys from `main` after CI
passes.

| What | URL |
| --- | --- |
| App (list, search, create, edit, delete contacts) | <https://contacthub-h4or.onrender.com> |
| Swagger UI (API docs, try it out) | <https://contacthub-h4or.onrender.com/swagger> |
| Health check | <https://contacthub-h4or.onrender.com/api/health> |
| Source | <https://github.com/GVLTodorov/ContactHub> |

> The free tier sleeps after about 15 minutes without traffic, so the first visit can take up to a
> minute to load. Data resets to five demo contacts on every restart or deploy (SQLite on an
> ephemeral disk).

| Project | Line coverage | Branch coverage |
| --- | --- | --- |
| Backend | ![Backend line coverage](https://github.com/GVLTodorov/ContactHub/raw/badges/coverage/backend/line.svg) | ![Backend branch coverage](https://github.com/GVLTodorov/ContactHub/raw/badges/coverage/backend/branch.svg) |
| Frontend | ![Frontend line coverage](https://github.com/GVLTodorov/ContactHub/raw/badges/coverage/frontend/line.svg) | ![Frontend branch coverage](https://github.com/GVLTodorov/ContactHub/raw/badges/coverage/frontend/branch.svg) |

![Demo: Swagger UI's Client button opens the app, then the seeded list is searched, a contact's phone and address are copied, a new contact is created (an invalid IBAN is fixed), edited and deleted.](https://github.com/GVLTodorov/ContactHub/raw/badges/demo/demo.gif)

CI regenerates the coverage badges and re-records the demo on every push to `main` and stores them
on the `badges` branch. Backend numbers combine unit and integration tests.

---

A small, single-user web app for managing personal contacts, built as an interview task
([requirements](Docs/Requirements.md), [design plan](Docs/plan.md)).

Each contact has a **first name**, **surname**, **date of birth**, **address** (street, postal
code, city, country), **phone number** and **IBAN**. You can list, view, create, edit and delete
contacts, and they are stored in SQLite. There are no accounts. The app has one anonymous user.

Stack: ASP.NET Core Web API (.NET 10, controllers, EF Core + SQLite, FluentValidation, hand-rolled
CQRS) and Angular (ngrx, PrimeNG with the Aura theme, Tailwind CSS v4 for layout), with Vitest,
NUnit and Playwright tests, Docker Compose and GitHub Actions.

## How to run

**Prerequisites:** .NET SDK 10 (pinned in `global.json`) and Node 24 (`.nvmrc`). Docker is only
needed for Compose.

| What | Command | Result |
| --- | --- | --- |
| Backend | `cd backend && dotnet run --project Api` | API on http://127.0.0.1:8080, Swagger UI at http://127.0.0.1:8080/swagger (its green **Client** button opens the app) |
| Frontend | `cd frontend && npm install && npm run start` | App on http://127.0.0.1:4700; `/api` is proxied to the backend (`frontend/proxy.conf.js`) |
| Whole stack | `docker compose up --build` | Backend on port 8080, frontend dev server on port 4700 |
| Backend tests | `cd backend && dotnet test` | NUnit. `Api.Tests`: unit tests, fails below 95% line or branch coverage (coverlet). `Api.Integration.Tests`: the API in memory through `WebApplicationFactory` |
| Frontend tests | `cd frontend && npm test -- --watch=false` | Vitest: reducer, selectors, effects, API service, validators, components. `npm run test:coverage` also writes `coverage/frontend/cobertura-coverage.xml` |
| E2E test | `dotnet run --project e2e/ContactHub.E2E` (from the repo root) | .NET console app using Microsoft.Playwright (Chromium); starts the backend and the dev server itself, exits non-zero on failure (first time: `dotnet run --project e2e/ContactHub.E2E -- install chromium`) |
| Demo video | `dotnet run --project e2e/ContactHub.Demo` (from the repo root) | Separate Playwright recorder, no assertions: starts the stack with demo contacts and records Swagger's Client button, search, copy, create, edit and delete to `e2e/artifacts/demo/demo.webm` (about 70 s; paced with 1 s steps, visible typing and a click marker) |
| Lint | `cd backend && dotnet format --verify-no-changes` · `cd e2e && dotnet format --verify-no-changes` · `cd frontend && npm run lint` | |

**Where the data lives.** The connection string is `Data Source=database/contacthub.db`. A
relative path is resolved against the running app's folder (`AppContext.BaseDirectory`), not the
working directory. Locally the file is
`backend/Api/bin/Debug/net10.0/database/contacthub.db`. Startup creates the folder and runs
`Database.Migrate()`, so the database is created on first run. Tests use `contacthub_test.db` in
the test project's output folder, and the e2e run uses `contacthub_e2e.db`, which is deleted
before each run. In Docker Compose the backend mounts the named volume `dbdata` on
`/app/database`. Running `dotnet clean` or deleting `bin/` wipes local data.

CI (`.github/workflows/ci.yml`) is one pipeline. On every push and pull request it runs the backend
(format, build, unit and integration tests with coverage), frontend (lint, tests with coverage,
build) and e2e jobs. On `main`, once those pass, it then:

- pushes the single image (API plus the built Angular app) to GHCR as `:<version>` and `:latest`;
- triggers a Render deploy through the `RENDER_DEPLOY_HOOK_URL` secret (skipped with a notice if unset);
- builds the coverage badges (ReportGenerator) and the demo GIF (`e2e/ContactHub.Demo`, ffmpeg), and
  pushes them to the orphan `badges` branch from a single job.

Backend coverage combines `Api.Tests` (coverlet.msbuild) and `Api.Integration.Tests`
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

## Architecture

### Backend: one project, four layers

`backend/Api/` is one ASP.NET Core project, split into folders:

```text
backend/Api/
├── Controllers/        ContactsController, HealthController
├── Middleware/         ValidationExceptionHandler, NotFoundExceptionHandler
├── Extensions/         SwaggerTopBar
├── Program.cs, Startup.cs, appsettings*.json
├── Application/
│   ├── Contacts/
│   │   ├── ContactDto.cs
│   │   ├── Commands/   CreateContact/, UpdateContact/, DeleteContact/   (Command, Validator, Handler)
│   │   └── Queries/    GetContactById/, GetContacts/                     (Query, Handler)
│   ├── Abstractions/   ICommand, IQuery, ICommandHandler, IQueryHandler, Unit
│   ├── Behaviors/      ValidationCommandHandlerDecorator
│   ├── Exceptions/     NotFoundException
│   └── DependencyInjection.cs   AddApplication()
├── Domain/
│   ├── Entities/       Contact (+ ContactValidator)
│   ├── ValueObjects/   Address, PhoneNumber, Iban
│   └── Exceptions/     DomainValidationException, ValidationKeys
└── Infrastructure/
    ├── Persistence/    AppDbContext, DemoDataSeeder, Configurations/, Migrations/
    └── DependencyInjection.cs   AddInfrastructure()
```

Namespaces follow the folders (`Api.Application.Contacts.Commands.CreateContact`, `Api.Domain.Entities`, ...).

- **Domain/**: the `Contact` aggregate and the value objects `Address`, `PhoneNumber` and `Iban`.
  Setters are private. `Contact.Create(...)` is the factory, and the behaviour methods `Rename`,
  `ChangeDateOfBirth`, `ChangeAddress`, `ChangePhoneNumber` and `ChangeIban` re-check the
  invariants and bump `UpdatedAt` only when a value actually changes. Ids are `Guid` v7, and the
  clock comes from an injected `TimeProvider`.
- **Application/**: CQRS without MediatR. `ICommand<T>`, `IQuery<T>`, `ICommandHandler<,>` and
  `IQueryHandler<,>` live in `Abstractions/`. Each use case has its own folder under
  `Application/Contacts/Commands/` or `Queries/`, with one file each for the command or query, its
  request validator and its handler. Queries read with `AsNoTracking`.
- **Infrastructure/**: `Persistence/` holds `AppDbContext`, `Configurations/ContactConfiguration`
  and `Migrations/`. `Address` is an EF complex property, `PhoneNumber` and `Iban` are stored
  through value converters, and `DateOfBirth` is a `DateOnly`.
- **Controllers/** and **Middleware/**: thin `[ApiController]` classes. Each action takes the
  handler via `[FromServices]`, dispatches the command or query, and maps the result to an HTTP
  status. Two `IExceptionHandler`s in `Middleware/` map validation errors to
  `400 ValidationProblem` and `NotFoundException` to `404 ProblemDetails`.

Hosting uses the classic `Program` + `Startup` model. `Startup.ConfigureServices` calls
`AddApplication()`, which scans the assembly, registers every handler, and wraps each command
handler in `ValidationCommandHandlerDecorator`, and `AddInfrastructure(configuration)`, which
registers the SQLite `AppDbContext`.

```mermaid
flowchart LR
    C[ContactsController] -->|command| V[ValidationCommandHandlerDecorator]
    V -->|FluentValidation request validators| H[Command handler]
    C -->|query| Q[Query handler]
    H -->|Contact.Create / Change*| D[Domain: Contact + value objects]
    H --> DB[(AppDbContext / SQLite)]
    Q -->|AsNoTracking projection| DB
    V -. ValidationException .-> X[ValidationExceptionHandler: 400]
    D -. DomainValidationException .-> X
```

### Validation layers

1. **Request (Application):** FluentValidation validators such as `CreateContactValidator`
   and `UpdateContactValidator`, run by the decorator before the handler. They reuse the
   domain rules (`PhoneNumber.IsValid`, `Iban.IsValid`, the length constants on the entity).
2. **Entity (Domain):** `ContactValidator` (an `AbstractValidator<Contact>`) runs inside
   `Contact.Create` and every behaviour method. It checks name lengths and that the date of birth
   is not in the future and not more than 150 years ago. A failure throws
   `DomainValidationException`.
3. **Value objects (Domain):** `Address.Create` (with `AddressValidator`), `PhoneNumber.Create`
   (an optional leading `+` followed by 7 to 15 digits, normalized) and `Iban.Create` (format check
   and ISO 13616 mod-97 checksum, normalized, `ToFormatted()` in groups of four). An invalid
   value object cannot be constructed.
4. **Client:** typed Reactive Forms in `ContactForm` check required fields and lengths, and use
   the `ibanValidator`, `phoneValidator` and `dateOfBirthValidator` from
   `components/contact-form/validators.ts`. Server `400` field errors (camelCase keys such as
   `address.city`) are shown under the matching inputs.

### Frontend: ngrx data flow

```mermaid
flowchart LR
    P[Page / component] -->|dispatch ContactsPageActions| S[Store]
    S --> E[ContactsEffects]
    E -->|HttpClient| A[ContactsApi]
    A -->|ContactsApiActions success / failure| R[Reducer + entity adapter]
    R --> SEL[Selectors]
    SEL -->|store.selectSignal| P
    E -. navigation, toasts .-> UI[Router / MessageService]
```

- **Actions** (`contacts.actions.ts`): `createActionGroup` defines page actions (opened, create,
  update and delete submitted, search changed) and API actions (success and failure for each call).
- **Effects** (`contacts.effects.ts`): call the typed `ContactsApi` (`src/app/api/contacts-api.ts`)
  and dispatch the API actions. Navigation and toasts are non-dispatching effects.
- **Reducer** (`contacts.reducer.ts`): a `createFeature` with an `@ngrx/entity` adapter (sorted by
  surname, then first name) plus `loadStatus`, `detailStatus`, `saveStatus`, `error`,
  `fieldErrors` and `query`.
- **Selectors:** feature selectors plus `selectAllContacts`, `selectFilteredContacts`,
  `selectIsEmpty`, and `selectSelectedContact` (from the router-store `id` param).
- **Components** read state as signals with `store.selectSignal(...)`. Routes are lazy-loaded with
  `loadComponent`.

## Requirements to code

| Requirement | Where |
| --- | --- |
| Data persisted | `backend/ContactHub.Infrastructure/DependencyInjection.cs` (SQLite path), `backend/Api/Startup.cs` (`Database.Migrate()`), `compose.yml` (`dbdata` volume) |
| EF Core | `backend/ContactHub.Infrastructure/Persistence/AppDbContext.cs`, `backend/ContactHub.Infrastructure/Persistence/Configurations/ContactConfiguration.cs`, `backend/ContactHub.Infrastructure/Persistence/Migrations/` |
| FluentValidation (models) | `backend/ContactHub.Application/Contacts/Commands/CreateContact/CreateContactValidator.cs`, `backend/ContactHub.Application/Contacts/Commands/UpdateContact/UpdateContactValidator.cs`, `backend/ContactHub.Application/Behaviors/ValidationCommandHandlerDecorator.cs` |
| FluentValidation (entities) | `backend/ContactHub.Domain/Entities/Contact.cs` (`ContactValidator`), `backend/ContactHub.Domain/ValueObjects/Address.cs` (`AddressValidator`) |
| CQRS | `backend/ContactHub.Application/Abstractions/`, `backend/ContactHub.Application/DependencyInjection.cs`, `backend/ContactHub.Application/Contacts/`, `backend/Api/Controllers/ContactsController.cs` |
| Rich Domain Model | `backend/ContactHub.Domain/Entities/Contact.cs`, `backend/ContactHub.Domain/ValueObjects/Address.cs`, `backend/ContactHub.Domain/ValueObjects/PhoneNumber.cs`, `backend/ContactHub.Domain/ValueObjects/Iban.cs`, `backend/ContactHub.Domain/Exceptions/DomainValidationException.cs` |
| Clean Architecture | `backend/ContactHub.Domain/`, `backend/ContactHub.Application/` (`Abstractions/IAppDbContext.cs`), `backend/ContactHub.Infrastructure/`, `backend/Api/` (web host), `backend/Api.Tests/ArchitectureTests.cs` |
| ngrx Store | `frontend/src/app/store/contacts/`, `frontend/src/app/app.config.ts`, `frontend/src/app/api/contacts-api.ts` |
| PrimeNG components | `frontend/src/app/app.config.ts` (`providePrimeNG`, Aura), `frontend/src/app/pages/contacts/contacts.html` (Table, ConfirmDialog), `frontend/src/app/components/contact-form/contact-form.html` (InputText, DatePicker, Fieldset), `frontend/src/app/components/layout/layout.html` |
| Playwright e2e test | `e2e/ContactHub.E2E/` (.NET console app, `Program.Main`), `e2e` job in `.github/workflows/ci.yml` |
| Tests | `backend/Api.Tests/` (unit, 95% coverage gate), `backend/Api.Integration.Tests/` (HTTP endpoints), `*.spec.ts` next to the frontend code |

## Contact list features

The `/contacts` page (`frontend/src/app/pages/contacts/`) is built for quickly finding a contact:

- **Search:** one box filters by name, phone number, IBAN or city. The term is stored in the ngrx
  `query` state through a `searchChanged` action, so it survives navigating to a contact and back.
  The memoized `selectFilteredContacts` selector (`frontend/src/app/store/contacts/`) does the
  filtering on the client. Matching ignores case and whitespace, so `de89 3704` finds a formatted
  IBAN. If nothing matches, the page shows a "No matches" state.
- **Sorting:** on wider screens you can sort by any column. By default the list is ordered by surname, then first
  name.
- **Paging:** the PrimeNG paginator pages through the filtered results.
- **Mobile:** on narrow screens (checked at 375 px) the table collapses to stacked rows and the form
  switches to one column.

All of this happens on the client. Server-side search and paging were left out on purpose,
because a personal contact list stays small.
