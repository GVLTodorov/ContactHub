# QA: requirements checklist

Each requirement from [requirements.md](requirements.md), where it is implemented, and how to check
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
<https://contacthub-web.onrender.com> (the first load can take up to a minute while the free-tier API
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
| SQLite database file, path resolved against the app folder | [ContactHub.Infrastructure/DependencyInjection.cs:28-36](../backend/ContactHub.Infrastructure/DependencyInjection.cs#L28-L36) |
| Migrations applied at startup (`Database.Migrate()`) | [ContactHub.Api/Program.cs:51](../backend/ContactHub.Api/Program.cs#L51) |
| Optional demo data (`SeedDemoData`) | [Program.cs:53](../backend/ContactHub.Api/Program.cs#L53), [DemoDataSeeder.cs](../backend/ContactHub.Infrastructure/Persistence/DemoDataSeeder.cs) |
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
| `UseSqlite` registration | [ContactHub.Infrastructure/DependencyInjection.cs:17](../backend/ContactHub.Infrastructure/DependencyInjection.cs#L17) |
| Entity mapping: complex property for `Address`, value converters for `PhoneNumber` and `Iban` (rebuilt with `FromTrusted`, so stricter rules never break reads), `Version` as concurrency token | [ContactConfiguration.cs:14-40](../backend/ContactHub.Infrastructure/Persistence/Configurations/ContactConfiguration.cs#L14-L40) |
| Migrations | [Persistence/Migrations/](../backend/ContactHub.Infrastructure/Persistence/Migrations/) |
| Queries and writes through EF Core (via `IAppDbContext`) | [GetContactsHandler.cs](../backend/ContactHub.Application/Contacts/Queries/GetContacts/GetContactsHandler.cs), [CreateContactHandler.cs](../backend/ContactHub.Application/Contacts/Commands/CreateContact/CreateContactHandler.cs) |

**Tests:** [MigrationTests.cs](../backend/ContactHub.Api.Tests/Infrastructure/MigrationTests.cs)

## 4. FluentValidation for models and entities

**Models (request commands, Application layer):**

| What | Where |
| --- | --- |
| Shared rules for create and update (`IContactInput`), including the nested address validator | [ContactInputValidator.cs:8-39](../backend/ContactHub.Application/Contacts/Commands/ContactInputValidator.cs#L8-L39), [CreateContactValidator.cs](../backend/ContactHub.Application/Contacts/Commands/CreateContact/CreateContactValidator.cs) |
| Update command validator (shared rules plus a required `version`) | [UpdateContactValidator.cs:10](../backend/ContactHub.Application/Contacts/Commands/UpdateContact/UpdateContactValidator.cs#L10) |
| Decorator that runs the validators before every command handler | [ValidationCommandHandlerDecorator.cs:8-18](../backend/ContactHub.Application/Behaviors/ValidationCommandHandlerDecorator.cs#L8-L18) |
| Validator and decorator registration | [ContactHub.Application/DependencyInjection.cs:13-28](../backend/ContactHub.Application/DependencyInjection.cs#L13-L28) |
| Validation errors mapped to `400 ValidationProblem` | [ValidationExceptionHandler.cs](../backend/ContactHub.Api/Middleware/ValidationExceptionHandler.cs) |

**Entities (Domain layer):**

| What | Where |
| --- | --- |
| `ContactValidator : AbstractValidator<Contact>` | [Contact.cs:127-141](../backend/ContactHub.Domain/Entities/Contact.cs#L127-L141) |
| Run on every create and change; throws `DomainValidationException` | [Contact.cs:117-124](../backend/ContactHub.Domain/Entities/Contact.cs#L117-L124) |
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
| `POST` and `PUT` bodies are HTTP request records, mapped to `CreateContactCommand` / `UpdateContactCommand` | [ContactRequests.cs](../backend/ContactHub.Api/Contracts/ContactRequests.cs) |
| Stale `version` on `PUT` returns `409 Conflict` | [UpdateContactHandler.cs](../backend/ContactHub.Application/Contacts/Commands/UpdateContact/UpdateContactHandler.cs), [ConflictExceptionHandler.cs](../backend/ContactHub.Api/Middleware/ConflictExceptionHandler.cs) |

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
| Private setters: state changes only through methods | [Contact.cs:16-27](../backend/ContactHub.Domain/Entities/Contact.cs#L16-L27) |
| Factory `Contact.Create(...)` that validates invariants | [Contact.cs:29-53](../backend/ContactHub.Domain/Entities/Contact.cs#L29-L53) |
| Behaviour methods `Update` (all fields at once), `Rename`, `ChangeDateOfBirth`, `ChangeAddress`, `ChangePhoneNumber`, `ChangeIban` | [Contact.cs:55-91](../backend/ContactHub.Domain/Entities/Contact.cs#L55-L91) |
| Change tracking: rolls back on invalid state, bumps `UpdatedAt` and `Version` only on a real change | [Contact.cs:93-115](../backend/ContactHub.Domain/Entities/Contact.cs#L93-L115) |
| Value object `Iban`: normalization, length per country (SWIFT IBAN registry, 89 countries), ISO 13616 mod-97 checksum, `ToFormatted()` | [Iban.cs](../backend/ContactHub.Domain/ValueObjects/Iban.cs) (country lengths at [line 10](../backend/ContactHub.Domain/ValueObjects/Iban.cs#L10), checks at [line 132](../backend/ContactHub.Domain/ValueObjects/Iban.cs#L132), checksum at [line 153](../backend/ContactHub.Domain/ValueObjects/Iban.cs#L153)) |
| Value object `PhoneNumber`: optional `+` and 7–15 digits, normalized | [PhoneNumber.cs](../backend/ContactHub.Domain/ValueObjects/PhoneNumber.cs) |
| Value object `Address` | [Address.cs](../backend/ContactHub.Domain/ValueObjects/Address.cs) |
| Domain exception | [DomainValidationException.cs](../backend/ContactHub.Domain/Exceptions/DomainValidationException.cs) |

**Tests:** [DomainTests.cs](../backend/ContactHub.Api.Tests/Domain/DomainTests.cs),
[DomainEdgeTests.cs](../backend/ContactHub.Api.Tests/Domain/DomainEdgeTests.cs)

## 7. ngrx Store

| What | Where |
| --- | --- |
| Store, router store, feature state and effects registered | [app.config.ts:20-23](../frontend/src/app/app.config.ts#L20-L23) |
| Actions: one `createActionGroup` per source (list, detail, new and edit pages, and `ContactsApiActions`) | [contacts.actions.ts:6-55](../frontend/src/app/store/contacts/contacts.actions.ts#L6-L55) |
| Effects: load, load one, create, update, delete, navigation, toasts | [contacts.effects.ts](../frontend/src/app/store/contacts/contacts.effects.ts) |
| Reducer: `createFeature` with an `@ngrx/entity` adapter | [contacts.reducer.ts:41](../frontend/src/app/store/contacts/contacts.reducer.ts#L41), [line 57](../frontend/src/app/store/contacts/contacts.reducer.ts#L57) |
| Selectors: `selectAllContacts`, `selectFilteredContacts`, `selectSelectedContact`, `selectIsEmpty` | [contacts.reducer.ts:135-150](../frontend/src/app/store/contacts/contacts.reducer.ts#L135-L150) |
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

An NUnit test project using Microsoft.Playwright (Chromium) drives the real app in a browser:
41 user flows in 50 test cases, about 1.5 minutes. Line coverage is enforced by the unit tests (95%
gate); e2e coverage is measured by user flows instead: every flow a user can reach in the UI has a
test below.

| What | Where |
| --- | --- |
| Shared setup: one browser per fixture, a fresh context per test, test data created through the API, screenshot and trace on failure | [E2ETest.cs](../e2e/ContactHub.E2E/E2ETest.cs) |
| Starts the backend and frontend once (`[SetUpFixture]`) | [E2ESetup.cs](../e2e/ContactHub.E2E/E2ESetup.cs), [Servers.cs](../e2e/ContactHub.E2E/Servers.cs) |
| Runs in CI (`e2e` job) | [ci.yml:63-83](../.github/workflows/ci.yml#L63-L83) |

Failure states the real API cannot produce on demand (server errors, a slow API, an empty database)
are simulated with Playwright network routing; everything else goes through the real backend.

| Area | User flow | Test |
| --- | --- | --- |
| App shell | Home page leads to the contacts | `NavigationTests.HomePage_LeadsToTheContacts` |
| App shell | Sidebar marks the current page; the logo goes home | `NavigationTests.Sidebar_MarksTheCurrentPage_AndTheLogoGoesHome` |
| App shell | "New contact" button opens the form | `NavigationTests.NewContactButton_OpensTheForm` |
| App shell | Unknown URL shows "Page not found" with a link home | `NavigationTests.UnknownRoute_ShowsPageNotFound_WithALinkHome` |
| App shell | Footer API status: checking, online, offline | `NavigationTests.ApiStatus_ShowsChecking_WhileTheHealthCheckRuns`, `ApiStatus_IsShownInTheFooter`, `ApiStatus_ShowsOffline_WhenTheHealthCheckFails` |
| App shell | Swagger's Client button opens the app | `NavigationTests.SwaggerClientButton_OpensTheApp` |
| List | Every column, IBAN in groups of four | `ContactListTests.ShowsEveryColumn_WithTheFormattedIban` |
| List | Search by name, phone, IBAN and city | `ContactListTests.Search_FindsByEachSupportedField` (4 cases) |
| List | No matches message and "Clear search" | `ContactListTests.NoMatches_ShowsMessage_AndClearSearchRestoresTheList` |
| List | "Clear" button empties the search box | `ContactListTests.ClearButton_EmptiesTheSearchBox` |
| List | Search survives opening a contact and coming back | `ContactListTests.Search_IsKeptWhenComingBackFromADetailPage` |
| List | Sorted by surname; the Name header reverses it | `ContactListTests.SortsBySurname_AndTheNameHeaderReversesIt` |
| List | Date of birth, Phone, IBAN and City headers sort | `ContactListTests.EachColumnHeader_SortsByItsColumn` (4 cases) |
| List | 10 rows per page, next page, 25 rows per page | `ContactListTests.Paginator_ShowsTenRowsPerPage_AndCanShowMore` |
| List | Loading skeleton while the API is slow | `ContactListTests.SlowApi_ShowsTheLoadingSkeleton_ThenTheList` |
| List | Empty database prompt leads to the form | `ContactListTests.EmptyDatabase_ShowsTheFirstContactPrompt` |
| List | Load error and Retry | `ContactListTests.LoadError_ShowsTheMessage_AndRetryRecovers` |
| List | Narrow screen stacks rows with inline labels | `MobileLayoutTests.NarrowScreen_StacksEachContactWithInlineLabels` |
| Create | Valid contact is saved, toasted and listed | `CreateContactTests.CreatesContact_AndShowsItInTheList` |
| Create | Empty submit shows nine required errors | `CreateContactTests.EmptySubmit_ShowsRequiredErrors_AndStaysOnTheForm` |
| Create | Invalid IBAN and phone are flagged, then saving works | `CreateContactTests.InvalidIbanAndPhone_AreFlagged_ThenSaveWorksOnceFixed` |
| Create | Date of birth in the future or over 150 years ago is rejected | `CreateContactTests.DateOfBirthOutOfRange_IsRejected` (2 cases) |
| Create | Server validation errors appear under the matching field | `CreateContactTests.ServerValidationErrors_AreShownUnderTheMatchingField` |
| Create | Cancel returns to the list | `CreateContactTests.Cancel_ReturnsToTheList` |
| Detail | Every field, age and formatted IBAN | `ContactDetailTests.OpensFromTheList_AndShowsEveryField` |
| Detail | Copy phone, IBAN and address to the clipboard | `ContactDetailTests.CopyButtons_PutTheValueOnTheClipboard` (3 cases) |
| Detail | Unknown id shows "Contact not found" with a way back | `ContactDetailTests.UnknownId_ShowsNotFound_WithAWayBack` |
| Detail | Load error and Retry | `ContactDetailTests.LoadError_ShowsTheMessage_AndRetryRecovers` |
| Edit | Save updates the detail page and shows a toast | `EditContactTests.Save_UpdatesTheDetailPage_AndShowsAToast` |
| Edit | Cancel returns to the detail page unchanged | `EditContactTests.Cancel_ReturnsToTheDetailPage_WithoutChanges` |
| Edit | Invalid value is flagged and nothing is saved | `EditContactTests.InvalidValue_IsFlagged_AndNothingIsSaved` |
| Edit | Someone else saved first: 409 message, their change kept | `EditContactTests.SomeoneElseSavedFirst_ShowsTheConflict_AndKeepsTheirChange` |
| Edit | Typed edits survive the background refresh | `EditContactTests.TypedEdits_SurviveTheBackgroundRefresh` |
| Edit | Load error message | `EditContactTests.LoadError_ShowsTheMessage` |
| Edit | Unknown id shows not found with "Back to list" | `EditContactTests.UnknownId_ShowsNotFound` |
| Delete | From the list, after confirming | `DeleteContactTests.FromTheList_AfterConfirming_RemovesTheRow` |
| Delete | Cancelling the confirmation keeps the row | `DeleteContactTests.FromTheList_Cancelling_KeepsTheRow` |
| Delete | Server failure shows an error toast and keeps the row | `DeleteContactTests.ServerFailure_ShowsAnErrorToast_AndKeepsTheRow` |
| Delete | From the detail page, back to the list without it | `DeleteContactTests.FromTheDetailPage_ReturnsToTheListWithoutIt` |

**How to check:** from the repo root, run `dotnet build e2e/ContactHub.E2E` and
`pwsh e2e/ContactHub.E2E/bin/Debug/net10.0/playwright.ps1 install chromium` once, then
`dotnet test e2e/ContactHub.E2E` (add `--filter "FullyQualifiedName~EditContactTests"` for one
area). On failure it saves a screenshot and a Playwright trace to `e2e/artifacts/` and attaches them
to the test result.

## 10. Git repository

<https://github.com/GVLTodorov/ContactHub>. CI runs on every push
([ci.yml](../.github/workflows/ci.yml)), and the app is deployed to Render from `main`.

---

## Beyond the requirements

| Extra | Where |
| --- | --- |
| Clean Architecture, one project per layer, enforced by architecture tests | [ArchitectureTests.cs](../backend/ContactHub.Api.Tests/Architecture/ArchitectureTests.cs), [architecture.md](architecture.md) |
| Client-side validation that mirrors the server rules (IBAN length per country and checksum, phone, date of birth) | [validators.ts](../frontend/src/app/components/contact-form/validators.ts), [contact-rules.json](../frontend/src/app/components/contact-form/contact-rules.json) (kept in step with the domain by [FrontendRulesTests.cs](../backend/ContactHub.Api.Tests/Domain/FrontendRulesTests.cs)) |
| Search, sorting and paging on the contact list | [contacts.html](../frontend/src/app/pages/contacts/contacts.html), [contacts.reducer.ts:138](../frontend/src/app/store/contacts/contacts.reducer.ts#L138) |
| Unit and integration tests with a 95% coverage gate | [ContactHub.Api.Tests/](../backend/ContactHub.Api.Tests/), [ContactHub.Api.Integration.Tests/](../backend/ContactHub.Api.Integration.Tests/) |
| Docker image, Compose, CI/CD to Render, coverage badges, demo video | [deployment.md](deployment.md) |
