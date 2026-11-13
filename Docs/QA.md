# QA: requirements checklist

Each requirement from [Requirements.md](Requirements.md), where it is implemented, and how to check
it. Links point to the exact files and lines. All items are **implemented**.

| # | Requirement | Status |
| --- | --- | --- |
| 1 | [Required data](#1-required-data) (first name, surname, D.O.B, address, phone number, IBAN) | ✅ |
| 2 | [Data is persisted](#2-data-is-persisted) | ✅ |
| 3 | [EF Core for database communication](#3-ef-core-for-database-communication) | ✅ SQLite |
| 4 | [FluentValidation for models and entities](#4-fluentvalidation-for-models-and-entities) | ✅ |
| 5 | [CQRS pattern](#5-cqrs-pattern) | ✅ |
| 6 | [Rich Domain Model](#6-rich-domain-model) | ✅ |
| 7 | [ngrx Store](#7-ngrx-store) | ✅ |
| 8 | [PrimeNG components](#8-primeng-components) | ✅ |
| 9 | [Playwright e2e test](#9-playwright-e2e-test) | ✅ |
| 10 | [Git repository](#10-git-repository) | ✅ |

Quick check without installing anything: open the live app at
<https://contacthub-h4or.onrender.com> (the first load can take up to a minute while the free tier
wakes up) and the API docs at <https://contacthub-h4or.onrender.com/swagger>.

---

## 1. Required data

| Field | Domain entity | Database column | API DTO | Form field |
| --- | --- | --- | --- | --- |
| First name | [Contact.cs:17](../backend/ContactHub.Domain/Entities/Contact.cs#L17) | `FirstName` | `firstName` | [contact-form.html:9](../frontend/src/app/components/contact-form/contact-form.html#L9) |
| Surname | [Contact.cs:18](../backend/ContactHub.Domain/Entities/Contact.cs#L18) | `Surname` | `surname` | [contact-form.html:16](../frontend/src/app/components/contact-form/contact-form.html#L16) |
| D.O.B | [Contact.cs:19](../backend/ContactHub.Domain/Entities/Contact.cs#L19) (`DateOnly`) | `DateOfBirth` | `dateOfBirth` | [contact-form.html:23](../frontend/src/app/components/contact-form/contact-form.html#L23) (`p-datepicker`) |
| Address | [Contact.cs:20](../backend/ContactHub.Domain/Entities/Contact.cs#L20), value object [Address.cs](../backend/ContactHub.Domain/ValueObjects/Address.cs) | `Address_Street`, `Address_PostalCode`, `Address_City`, `Address_Country` | `address { street, postalCode, city, country }` | [contact-form.html:51](../frontend/src/app/components/contact-form/contact-form.html#L51) (`p-fieldset`) |
| Phone number | [Contact.cs:21](../backend/ContactHub.Domain/Entities/Contact.cs#L21), value object [PhoneNumber.cs](../backend/ContactHub.Domain/ValueObjects/PhoneNumber.cs) | `PhoneNumber` | `phoneNumber` | [contact-form.html:37](../frontend/src/app/components/contact-form/contact-form.html#L37) |
| IBAN | [Contact.cs:22](../backend/ContactHub.Domain/Entities/Contact.cs#L22), value object [Iban.cs](../backend/ContactHub.Domain/ValueObjects/Iban.cs) | `Iban` | `iban`, `ibanFormatted` | [contact-form.html:44](../frontend/src/app/components/contact-form/contact-form.html#L44) |

- Table definition: [20261112075449_AddContacts.cs:14-29](../backend/ContactHub.Infrastructure/Persistence/Migrations/20261112075449_AddContacts.cs#L14-L29)
- DTO: [ContactDto.cs](../backend/ContactHub.Application/Contacts/ContactDto.cs)
- Frontend model and API client: [contacts-api.ts](../frontend/src/app/api/contacts-api.ts)

**How to check:** open **New contact** in the app. The form asks for all six fields, and the saved
contact shows them on its detail page.

## 2. Data is persisted

| What | Where |
| --- | --- |
| SQLite database file, path resolved against the app folder | [ContactHub.Infrastructure/DependencyInjection.cs:16-19](../backend/ContactHub.Infrastructure/DependencyInjection.cs#L16-L19) |
| Migrations applied at startup (`Database.Migrate()`) | [ContactHub.Api/Startup.cs:35](../backend/ContactHub.Api/Startup.cs#L35) |
| Optional demo data (`SeedDemoData`) | [Startup.cs:37](../backend/ContactHub.Api/Startup.cs#L37), [DemoDataSeeder.cs](../backend/ContactHub.Infrastructure/Persistence/DemoDataSeeder.cs) |
| Docker Compose named volume `dbdata` on `/app/database` | [compose.yml:12](../compose.yml#L12), [compose.yml:26](../compose.yml#L26) |
| Image creates the writable database folder | [Dockerfile:10](../Dockerfile#L10) |

**How to check:** run `cd backend && dotnet run --project ContactHub.Api` and create a contact.
Stop and restart the backend. The contact is still listed. The file is
`backend/ContactHub.Api/bin/Debug/net10.0/database/contacthub.db`. The live Render demo resets on
each deploy by design, because its disk is ephemeral.

## 3. EF Core for database communication

| What | Where |
| --- | --- |
| `AppDbContext` with `DbSet<Contact>` | [AppDbContext.cs:7-12](../backend/ContactHub.Infrastructure/Persistence/AppDbContext.cs#L7-L12) |
| `UseSqlite` registration | [ContactHub.Infrastructure/DependencyInjection.cs:19](../backend/ContactHub.Infrastructure/DependencyInjection.cs#L19) |
| Entity mapping: complex property for `Address`, value converters for `PhoneNumber` and `Iban` | [ContactConfiguration.cs:14-31](../backend/ContactHub.Infrastructure/Persistence/Configurations/ContactConfiguration.cs#L14-L31) |
| Migrations | [Persistence/Migrations/](../backend/ContactHub.Infrastructure/Persistence/Migrations/) |
| Queries and writes through EF Core (via `IAppDbContext`) | [GetContactsHandler.cs](../backend/ContactHub.Application/Contacts/Queries/GetContacts/GetContactsHandler.cs), [CreateContactHandler.cs](../backend/ContactHub.Application/Contacts/Commands/CreateContact/CreateContactHandler.cs) |

**Tests:** [MigrationTests.cs](../backend/ContactHub.Api.Tests/Infrastructure/MigrationTests.cs)

## 4. FluentValidation for models and entities

**Models (request commands, Application layer):**

| What | Where |
| --- | --- |
| Create command validator, including the nested address validator | [CreateContactValidator.cs:7-40](../backend/ContactHub.Application/Contacts/Commands/CreateContact/CreateContactValidator.cs#L7-L40) |
| Update command validator (shares the rules) | [UpdateContactValidator.cs:5](../backend/ContactHub.Application/Contacts/Commands/UpdateContact/UpdateContactValidator.cs#L5) |
| Decorator that runs the validators before every command handler | [ValidationCommandHandlerDecorator.cs:8-18](../backend/ContactHub.Application/Behaviors/ValidationCommandHandlerDecorator.cs#L8-L18) |
| Validator and decorator registration | [ContactHub.Application/DependencyInjection.cs:13-28](../backend/ContactHub.Application/DependencyInjection.cs#L13-L28) |
| Validation errors mapped to `400 ValidationProblem` | [ValidationExceptionHandler.cs](../backend/ContactHub.Api/Middleware/ValidationExceptionHandler.cs) |

**Entities (Domain layer):**

| What | Where |
| --- | --- |
| `ContactValidator : AbstractValidator<Contact>` | [Contact.cs:103-115](../backend/ContactHub.Domain/Entities/Contact.cs#L103-L115) |
| Run on every create and change; throws `DomainValidationException` | [Contact.cs:93-100](../backend/ContactHub.Domain/Entities/Contact.cs#L93-L100) |
| `AddressValidator : AbstractValidator<Address>` | [Address.cs:42-49](../backend/ContactHub.Domain/ValueObjects/Address.cs#L42-L49) |

**How to check:** in Swagger, `POST /api/contacts` with an invalid IBAN (for example
`GB00WEST12345698765432`) or an empty first name. The response is `400` with field errors.
**Tests:** [ValidatorTests.cs](../backend/ContactHub.Api.Tests/Application/ValidatorTests.cs),
[ContactValidationTests.cs](../backend/ContactHub.Api.Integration.Tests/ContactValidationTests.cs)

## 5. CQRS pattern

Implemented by hand (no MediatR). Commands change state, and queries only read (`AsNoTracking`).

| What | Where |
| --- | --- |
| Abstractions: `ICommand<T>`, `ICommandHandler<,>`, `IQuery<T>`, `IQueryHandler<,>` | [Application/Abstractions/](../backend/ContactHub.Application/Abstractions/) |
| Commands: Create, Update, Delete (command, validator, handler per folder) | [Contacts/Commands/](../backend/ContactHub.Application/Contacts/Commands/) |
| Queries: GetContacts, GetContactById | [Contacts/Queries/](../backend/ContactHub.Application/Contacts/Queries/) |
| Controller dispatches commands and queries to handlers (`[FromServices]`) | [ContactsController.cs](../backend/ContactHub.Api/Controllers/ContactsController.cs) |

**Endpoints:**

| Method | Path | Handler |
| --- | --- | --- |
| `GET` | `/api/contacts` | `GetContactsHandler` |
| `GET` | `/api/contacts/{id}` | `GetContactByIdHandler` |
| `POST` | `/api/contacts` | `CreateContactHandler` |
| `PUT` | `/api/contacts/{id}` | `UpdateContactHandler` |
| `DELETE` | `/api/contacts/{id}` | `DeleteContactHandler` |

**Tests:** [HandlerTests.cs](../backend/ContactHub.Api.Tests/Application/HandlerTests.cs),
[ContactsEndpointTests.cs](../backend/ContactHub.Api.Integration.Tests/ContactsEndpointTests.cs)

## 6. Rich Domain Model

| What | Where |
| --- | --- |
| Private setters: state changes only through methods | [Contact.cs:16-24](../backend/ContactHub.Domain/Entities/Contact.cs#L16-L24) |
| Factory `Contact.Create(...)` that validates invariants | [Contact.cs:26-49](../backend/ContactHub.Domain/Entities/Contact.cs#L26-L49) |
| Behaviour methods `Rename`, `ChangeDateOfBirth`, `ChangeAddress`, `ChangePhoneNumber`, `ChangeIban` | [Contact.cs:51-68](../backend/ContactHub.Domain/Entities/Contact.cs#L51-L68) |
| Change tracking: rolls back on invalid state, bumps `UpdatedAt` only on a real change | [Contact.cs:70-91](../backend/ContactHub.Domain/Entities/Contact.cs#L70-L91) |
| Value object `Iban`: normalization, ISO 13616 mod-97 checksum, `ToFormatted()` | [Iban.cs](../backend/ContactHub.Domain/ValueObjects/Iban.cs) (checksum at [line 59](../backend/ContactHub.Domain/ValueObjects/Iban.cs#L59)) |
| Value object `PhoneNumber`: optional `+` and 7–15 digits, normalized | [PhoneNumber.cs](../backend/ContactHub.Domain/ValueObjects/PhoneNumber.cs) |
| Value object `Address` | [Address.cs](../backend/ContactHub.Domain/ValueObjects/Address.cs) |
| Domain exception | [DomainValidationException.cs](../backend/ContactHub.Domain/Exceptions/DomainValidationException.cs) |

**Tests:** [DomainTests.cs](../backend/ContactHub.Api.Tests/Domain/DomainTests.cs),
[DomainEdgeTests.cs](../backend/ContactHub.Api.Tests/Domain/DomainEdgeTests.cs)

## 7. ngrx Store

| What | Where |
| --- | --- |
| Store, router store, feature state and effects registered | [app.config.ts:20-23](../frontend/src/app/app.config.ts#L20-L23) |
| Actions: `ContactsPageActions` and `ContactsApiActions` (`createActionGroup`) | [contacts.actions.ts:4](../frontend/src/app/store/contacts/contacts.actions.ts#L4), [line 18](../frontend/src/app/store/contacts/contacts.actions.ts#L18) |
| Effects: load, load one, create, update, delete, navigation, toasts | [contacts.effects.ts](../frontend/src/app/store/contacts/contacts.effects.ts) |
| Reducer: `createFeature` with an `@ngrx/entity` adapter | [contacts.reducer.ts:35](../frontend/src/app/store/contacts/contacts.reducer.ts#L35), [line 51](../frontend/src/app/store/contacts/contacts.reducer.ts#L51) |
| Selectors: `selectAllContacts`, `selectFilteredContacts`, `selectSelectedContact`, `selectIsEmpty` | [contacts.reducer.ts:121-132](../frontend/src/app/store/contacts/contacts.reducer.ts#L121-L132) |
| Components read state with `store.selectSignal(...)` | [pages/contacts/contacts.ts](../frontend/src/app/pages/contacts/contacts.ts), [contact-detail.ts](../frontend/src/app/pages/contact-detail/contact-detail.ts), [contact-edit.ts](../frontend/src/app/pages/contact-edit/contact-edit.ts), [contact-new.ts](../frontend/src/app/pages/contact-new/contact-new.ts) |

**Tests:** [contacts.reducer.spec.ts](../frontend/src/app/store/contacts/contacts.reducer.spec.ts),
[contacts.selectors.spec.ts](../frontend/src/app/store/contacts/contacts.selectors.spec.ts),
[contacts.effects.spec.ts](../frontend/src/app/store/contacts/contacts.effects.spec.ts)

## 8. PrimeNG components

Theme: `providePrimeNG` with the Aura preset in [app.config.ts:26-28](../frontend/src/app/app.config.ts#L26-L28).

| Component | Where |
| --- | --- |
| `p-table` (sorting, paging), `p-sort-icon` | [contacts.html:49](../frontend/src/app/pages/contacts/contacts.html#L49), [line 59](../frontend/src/app/pages/contacts/contacts.html#L59) |
| `p-confirmdialog` (delete confirmation) | [contacts.html:105](../frontend/src/app/pages/contacts/contacts.html#L105), [contact-detail.html:90](../frontend/src/app/pages/contact-detail/contact-detail.html#L90) |
| `pInputText`, `p-datepicker`, `p-fieldset`, `p-message`, `p-button` (form) | [contact-form.html:3-66](../frontend/src/app/components/contact-form/contact-form.html#L3-L66) |
| `p-card`, `p-avatar`, `p-skeleton` (detail page) | [contact-detail.html:18-36](../frontend/src/app/pages/contact-detail/contact-detail.html#L18-L36) |
| `p-toolbar`, `p-toast`, `p-tag` (layout) | [layout.html:1-41](../frontend/src/app/components/layout/layout.html#L1-L41) |

## 9. Playwright e2e test

| What | Where |
| --- | --- |
| Test: .NET console app using Microsoft.Playwright (Chromium) | [e2e/ContactHub.E2E/Program.cs](../e2e/ContactHub.E2E/Program.cs) |
| Scenario: fill the New contact form, save, check the new row and formatted IBAN in the list | [Program.cs:43-59](../e2e/ContactHub.E2E/Program.cs#L43-L59) |
| Starts the backend and frontend itself | [Servers.cs](../e2e/ContactHub.E2E/Servers.cs) |
| Runs in CI (`e2e` job) | [ci.yml:63-78](../.github/workflows/ci.yml#L63-L78) |

**How to check:** from the repo root, run
`dotnet run --project e2e/ContactHub.E2E -- install chromium` once, then
`dotnet run --project e2e/ContactHub.E2E`. It prints `E2E PASSED` and exits with `0`. On failure
it saves a screenshot and a trace to `e2e/artifacts/`.

## 10. Git repository

<https://github.com/GVLTodorov/ContactHub>. CI runs on every push
([ci.yml](../.github/workflows/ci.yml)), and the app is deployed to Render from `main`.

---

## Beyond the requirements

| Extra | Where |
| --- | --- |
| Clean Architecture, one project per layer, enforced by architecture tests | [ArchitectureTests.cs](../backend/ContactHub.Api.Tests/Architecture/ArchitectureTests.cs), [architecture.md](architecture.md) |
| Client-side validation that mirrors the server rules (IBAN, phone, date of birth) | [validators.ts](../frontend/src/app/components/contact-form/validators.ts) |
| Search, sorting and paging on the contact list | [contacts.html](../frontend/src/app/pages/contacts/contacts.html), [contacts.reducer.ts:124](../frontend/src/app/store/contacts/contacts.reducer.ts#L124) |
| Unit and integration tests with a 95% coverage gate | [ContactHub.Api.Tests/](../backend/ContactHub.Api.Tests/), [ContactHub.Api.Integration.Tests/](../backend/ContactHub.Api.Integration.Tests/) |
| Docker image, Compose, CI/CD to Render, coverage badges, demo video | [deployment.md](deployment.md) |
