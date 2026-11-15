# <img src="frontend/public/favicon.svg" alt="" width="40" align="top"> ContactHub

A contact manager built as an interview task ([requirements](docs/requirements.md)): ASP.NET Core
(.NET 10) API with SQLite and an Angular UI.

**Live demo:** [app](https://contacthub-web.onrender.com) · [Swagger](https://contacthub-h4or.onrender.com/swagger)
(free hosting, the first request can take up to a minute)

| Suite | Line coverage | Branch coverage |
| --- | --- | --- |
| Backend unit | ![Backend unit line coverage](https://github.com/GVLTodorov/ContactHub/raw/badges/coverage/backend/unit/line.svg) | ![Backend unit branch coverage](https://github.com/GVLTodorov/ContactHub/raw/badges/coverage/backend/unit/branch.svg) |
| Backend integration | ![Backend integration line coverage](https://github.com/GVLTodorov/ContactHub/raw/badges/coverage/backend/integration/line.svg) | ![Backend integration branch coverage](https://github.com/GVLTodorov/ContactHub/raw/badges/coverage/backend/integration/branch.svg) |
| Backend E2E | ![Backend E2E line coverage](https://github.com/GVLTodorov/ContactHub/raw/badges/coverage/backend/e2e/line.svg) | ![Backend E2E branch coverage](https://github.com/GVLTodorov/ContactHub/raw/badges/coverage/backend/e2e/branch.svg) |
| Frontend unit | ![Frontend unit line coverage](https://github.com/GVLTodorov/ContactHub/raw/badges/coverage/frontend/unit/line.svg) | ![Frontend unit branch coverage](https://github.com/GVLTodorov/ContactHub/raw/badges/coverage/frontend/unit/branch.svg) |
| Frontend E2E | ![Frontend E2E line coverage](https://github.com/GVLTodorov/ContactHub/raw/badges/coverage/frontend/e2e/line.svg) | ![Frontend E2E branch coverage](https://github.com/GVLTodorov/ContactHub/raw/badges/coverage/frontend/e2e/branch.svg) |

![Demo: Swagger UI's Client button opens the app, then the seeded list is searched, a contact's phone and address are copied, a new contact is created (an invalid IBAN is fixed), edited and deleted.](https://github.com/GVLTodorov/ContactHub/raw/badges/demo/demo.gif)

## How to run

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download) and [Node 24](https://nodejs.org).
The database is created automatically on first run.

### Visual Studio

1. Once, install the frontend packages: `cd frontend && npm install`
2. Open `backend/ContactHub.sln`.
3. Choose the **http + frontend** profile next to the green Start button and press **F5**.

Swagger opens at http://127.0.0.1:8080/swagger. Its **Client** button opens the app at
http://127.0.0.1:4700 (give Angular about 20 seconds to build).

### VS Code

1. Open the repository folder and install the recommended **C#** extension.
2. Choose **Backend + Frontend** in Run and Debug and press **F5**.

Swagger opens at http://127.0.0.1:8080/swagger, the app at http://127.0.0.1:4700.

### Docker

```sh
docker compose up --build
```

App at http://127.0.0.1:4700, Swagger at http://127.0.0.1:8080/swagger.

---

More detail: [Architecture](docs/architecture.md) · [Requirements checklist](docs/qa.md) ·
[Tests, CI and deployment](docs/deployment.md)
