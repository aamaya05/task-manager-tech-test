# TaskManager

A full-stack task management application built with **ASP.NET Core 9 Web API** (Clean Architecture, raw ADO.NET) and **Angular 20 + Bootstrap 5**, backed by **PostgreSQL 16**.

---

## Table of Contents

- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Quick Start — Docker Compose](#quick-start--docker-compose)
- [Demo Credentials](#demo-credentials)
- [Manual Setup](#manual-setup)
  - [1. Start PostgreSQL](#1-start-postgresql)
  - [2. Run Database Migrations](#2-run-database-migrations)
  - [3. Run the Backend API](#3-run-the-backend-api)
  - [4. Run the Frontend](#4-run-the-frontend)
- [Swagger UI](#swagger-ui)
- [Demo Seed Endpoint](#demo-seed-endpoint)
- [Debugging with VS Code and Docker](#debugging-with-vs-code-and-docker)
  - [Prerequisites](#debugging-prerequisites)
  - [Step 1 — Start the debug stack](#step-1--start-the-debug-stack)
  - [Step 2 — Wait for the API to be ready](#step-2--wait-for-the-api-to-be-ready)
  - [Step 3 — Set breakpoints](#step-3--set-breakpoints)
  - [Step 4 — Attach the debugger](#step-4--attach-the-debugger)
  - [Step 5 — Trigger a breakpoint](#step-5--trigger-a-breakpoint)
  - [Stop the debug stack](#stop-the-debug-stack)
- [Running Tests](#running-tests)
- [Environment Configuration](#environment-configuration)
- [API Overview](#api-overview)


## Tech Stack

| Layer | Technology |
|---|---|
| Language | C# (.NET SDK 9.0.314) |
| Web Framework | ASP.NET Core Web API (.NET 9) |
| Data Access | Raw ADO.NET via Npgsql — no ORM |
| Database | PostgreSQL 16 |
| Auth | JWT Bearer (HS256) + BCrypt password hashing |
| Frontend | Angular 20 LTS + TypeScript |
| UI Styling | Bootstrap 5 |
| Testing | xUnit, Moq, FluentAssertions, Testcontainers |
| Containerization | Docker + Docker Compose |


## Prerequisites

### macOS

```bash
# .NET SDK 9.0.314
# Download from: https://dotnet.microsoft.com/download/dotnet/9.0
# Or via Homebrew:
brew install dotnet@9

dotnet --version
# Expected: 9.0.314

# Node.js 20+ LTS
brew install node@20
node --version    # v20.x.x or higher

# Angular CLI 20
npm install -g @angular/cli@20

# Docker Desktop
# https://docs.docker.com/desktop/install/mac-install/
docker --version
docker compose version
```

### Windows

```powershell
# .NET SDK 9.0.314
# Download from: https://dotnet.microsoft.com/download/dotnet/9.0
# Run the installer, then verify:
dotnet --version
# Expected: 9.0.314

# Node.js 20+ LTS
winget install OpenJS.NodeJS.LTS
# Or download from: https://nodejs.org/en/download/
node --version    # v20.x.x or higher

# Angular CLI 20
npm install -g @angular/cli@20

# Docker Desktop
# https://docs.docker.com/desktop/install/windows-install/
docker --version
docker compose version
```

> **Windows note:** All commands below work in **PowerShell**, **Git Bash**, or **WSL2**.
> Replace forward slashes with backslashes where needed in PowerShell.

## Quick Start — Docker Compose

The fastest way to run everything. Starts PostgreSQL, runs all migrations, starts the API, and serves the Angular app.

```bash
# From the repository root, run in the background
docker compose up --build -d
```

| Service | URL |
|---|---|
| Angular UI | http://localhost:4200 |
| REST API | http://localhost:5001 |
| Swagger UI | http://localhost:5001/swagger |
| PostgreSQL | localhost:5432 (db: `taskmanager`, user: `postgres`, password: `postgres`) |

> **macOS note:** Port 5000 is reserved by the system AirPlay Receiver service. Docker Compose maps the API to **5001** on the host (`5001:5000`) to avoid this conflict. The manual `dotnet run` setup still uses 5000 since it runs outside Docker.

Stop all services:

```bash
docker compose down

# Also remove the database volume:
docker compose down -v
```

## Demo Credentials

These are created by the SQL seed script (`003_seed_data.sql`) or by calling `POST /api/demo/seed`.

| Email | Password |
|---|---|
| `demo@taskmanager.io` | `Demo1234!` |
| `admin@taskmanager.io` | `Admin1234!` |

## Manual Setup

### 1. Start PostgreSQL

**macOS / Linux:**

```bash
docker run \
  --name taskmanager-db \
  -e POSTGRES_DB=taskmanager \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  -d postgres:16
```

**Windows (PowerShell):**

```powershell
docker run `
  --name taskmanager-db `
  -e POSTGRES_DB=taskmanager `
  -e POSTGRES_USER=postgres `
  -e POSTGRES_PASSWORD=postgres `
  -p 5432:5432 `
  -d postgres:16
```

Verify the container is running:

```bash
docker ps
```

---

### 2. Run Database Migrations

Three plain SQL scripts must be applied in order.

**Option A — using `psql` locally:**

```bash
# macOS / Linux
cd backend

psql "host=localhost port=5432 dbname=taskmanager user=postgres password=postgres" \
  -f src/TaskManager.Infrastructure/Migrations/001_create_users_table.sql

psql "host=localhost port=5432 dbname=taskmanager user=postgres password=postgres" \
  -f src/TaskManager.Infrastructure/Migrations/002_create_tasks_table.sql

psql "host=localhost port=5432 dbname=taskmanager user=postgres password=postgres" \
  -f src/TaskManager.Infrastructure/Migrations/003_seed_data.sql
```

```powershell
# Windows (PowerShell)
cd backend

$conn = "host=localhost port=5432 dbname=taskmanager user=postgres password=postgres"

psql $conn -f src\TaskManager.Infrastructure\Migrations\001_create_users_table.sql
psql $conn -f src\TaskManager.Infrastructure\Migrations\002_create_tasks_table.sql
psql $conn -f src\TaskManager.Infrastructure\Migrations\003_seed_data.sql
```

**Option B — via Docker (no local `psql` required):**

```bash
# macOS / Linux
docker exec -i taskmanager-db psql -U postgres -d taskmanager \
  < backend/src/TaskManager.Infrastructure/Migrations/001_create_users_table.sql

docker exec -i taskmanager-db psql -U postgres -d taskmanager \
  < backend/src/TaskManager.Infrastructure/Migrations/002_create_tasks_table.sql

docker exec -i taskmanager-db psql -U postgres -d taskmanager \
  < backend/src/TaskManager.Infrastructure/Migrations/003_seed_data.sql
```

```powershell
# Windows (PowerShell)
Get-Content backend\src\TaskManager.Infrastructure\Migrations\001_create_users_table.sql |
  docker exec -i taskmanager-db psql -U postgres -d taskmanager

Get-Content backend\src\TaskManager.Infrastructure\Migrations\002_create_tasks_table.sql |
  docker exec -i taskmanager-db psql -U postgres -d taskmanager

Get-Content backend\src\TaskManager.Infrastructure\Migrations\003_seed_data.sql |
  docker exec -i taskmanager-db psql -U postgres -d taskmanager
```

---

### 3. Run the Backend API

```bash
# From the repository root
cd backend/src/TaskManager.WebApi

dotnet restore
dotnet run
```

The API starts at **http://localhost:5000**.

Verify it is running:

```bash
# macOS / Linux
curl http://localhost:5000/api/auth/health
# Expected: {"status":"healthy","timestamp":"..."}
```

```powershell
# Windows (PowerShell)
Invoke-RestMethod http://localhost:5000/api/auth/health
```

---

### 4. Run the Frontend

Open a **new terminal** from the repository root:

```bash
cd frontend

npm install

ng serve
# Or, if the Angular CLI is not installed globally:
npx ng serve
```

The app is available at **http://localhost:4200**.

The default route redirects to `/login`. After a successful login, the app redirects to `/tasks`.

---

## Swagger UI

The API ships with Swagger UI enabled in all environments.

| Mode | URL |
|---|---|
| Docker Compose | http://localhost:5001/swagger |
| Manual `dotnet run` | http://localhost:5000/swagger |

**Authenticating in Swagger UI:**

1. Call `POST /api/auth/login` with valid credentials to obtain a JWT token.
2. Click the **Authorize** button at the top right of the Swagger UI page.
3. Enter `<your-token>` in the Bearer field and click **Authorize**.
4. All subsequent requests from Swagger UI will include the `Authorization: Bearer` header automatically.

---

## Demo Seed Endpoint

A protected endpoint is available to seed two demo users and sample tasks directly into the database without running SQL scripts manually. It is intended for demonstration and testing purposes only.

**Step 1 — Register any user and log in to obtain a JWT:**

```bash
# Register
curl -X POST http://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"setup","email":"setup@example.com","password":"Setup1234!"}'

# Login
curl -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"setup@example.com","password":"Setup1234!"}'
```

**Step 2 — Call the seed endpoint with the token:**

```bash
curl -X POST http://localhost:5001/api/demo/seed \
  -H "Authorization: Bearer <your-token-here>"
```

**Response:**

```json
{
  "message": "Demo data seeded successfully.",
  "seeded": [
    "demo_user (demo@taskmanager.io / Demo1234!)",
    "5 sample tasks for demo_user",
    "admin (admin@taskmanager.io / Admin1234!)",
    "3 sample tasks for admin"
  ],
  "credentials": [
    { "username": "demo_user", "email": "demo@taskmanager.io", "password": "Demo1234!" },
    { "username": "admin",     "email": "admin@taskmanager.io", "password": "Admin1234!" }
  ]
}
```

The endpoint is **idempotent** — calling it multiple times is safe; it skips users that already exist.

---

## Debugging with VS Code and Docker

VS Code attaches to the .NET process running **inside** the container using `vsdbg` (the .NET remote debugger). A separate debug-targeted Docker Compose override is provided so the production compose file is never modified.

### How it works

```
VS Code (macOS host)
  └── C# extension (ms-dotnettools.csharp)
        └── attaches via Docker exec pipe
              └── vsdbg inside taskmanager-api-debug container
                    └── .NET process (Debug build, dotnet run)
```

The container mounts your local `backend/src` folder read-only, which allows VS Code to resolve container file paths back to your local source files so breakpoints land correctly.

---

### Debugging Prerequisites

Install the following VS Code extensions (VS Code will prompt automatically via `.vscode/extensions.json`):

| Extension | ID |
|---|---|
| C# Dev Kit | `ms-dotnettools.csdevkit` |
| C# | `ms-dotnettools.csharp` |
| Docker | `ms-azuretools.vscode-docker` |

---

### Step 1 — Start the debug stack

The debug stack uses `docker-compose.debug.yml` as an override on top of `docker-compose.yml`. It replaces only the `api` service with a debug build that includes `vsdbg` and exposes port `5678` for the debugger.

**Option A — via VS Code task:**

Open the Command Palette (`Cmd+Shift+P`) → **Tasks: Run Task** → **docker-compose-debug-up**

**Option B — terminal:**

```bash
# From the repository root
docker compose -f docker-compose.yml -f docker-compose.debug.yml up --build -d
```

> **If you see a read-only filesystem error** on startup, the named volumes that shadow `obj/` and `bin/` may be stale. Remove them and rebuild:
> ```bash
> docker compose -f docker-compose.yml -f docker-compose.debug.yml down -v
> docker compose -f docker-compose.yml -f docker-compose.debug.yml up --build -d
> ```

| Service | URL |
|---|---|
| Angular UI | http://localhost:4200 |
| REST API (debug) | http://localhost:5001 |
| Swagger UI (debug) | http://localhost:5001/swagger |
| PostgreSQL | localhost:5432 |

> The debug image runs `dotnet run --configuration Debug` directly from source — there is no publish step. The first startup takes longer than the production image while it compiles.

---

### Step 2 — Wait for the API to be ready

```bash
docker logs taskmanager-api-debug -f
```

Wait until you see:

```
Now listening on: http://[::]:5000
Application started. Press Ctrl+C to shut down.
```

---

### Step 3 — Set breakpoints

Open any source file under `backend/src/` in VS Code and click the gutter (left of the line number) to place a red breakpoint dot. Good starting points:

- `backend/src/TaskManager.WebApi/Controllers/TasksController.cs` — any action method
- `backend/src/TaskManager.Application/Tasks/Handlers/CreateTaskHandler.cs` — `Handle` method
- `backend/src/TaskManager.Infrastructure/Persistence/PostgresTaskRepository.cs` — any repository method

---

### Step 4 — Attach the debugger

1. Open the **Run and Debug** panel (`Cmd+Shift+D` on macOS, `Ctrl+Shift+D` on Windows/Linux).
2. Select **"Docker: Attach to API"** from the configuration dropdown at the top.
3. Press **F5** (or click the green play button).
4. VS Code will query the container for running processes and show a picker — select the process named **`TaskManager.WebApi`** or **`dotnet`**.

The status bar at the bottom of VS Code turns orange, confirming the debugger is attached.

---

### Step 5 — Trigger a breakpoint

Make any HTTP request that hits the code where your breakpoint is set. Use any of:

- **Swagger UI** at http://localhost:5001/swagger
- **curl** from a terminal
- **The Angular frontend** at http://localhost:4200

VS Code will pause execution at the breakpoint. You can then:

- Inspect variables in the **Variables** panel
- Step through code with **F10** (step over), **F11** (step into), **Shift+F11** (step out)
- Evaluate expressions in the **Debug Console**
- Continue execution with **F5**

---

### Stop the debug stack

**Option A — via VS Code task:**

Command Palette → **Tasks: Run Task** → **docker-compose-debug-down**

**Option B — terminal:**

```bash
docker compose -f docker-compose.yml -f docker-compose.debug.yml down
```

---

## Running Tests

### All backend tests

```bash
cd backend
dotnet test
```

### By layer

```bash
# Domain unit tests — no I/O, no Docker required
dotnet test tests/TaskManager.Domain.Tests

# Application unit tests — mocked dependencies, no Docker required
dotnet test tests/TaskManager.Application.Tests

# Infrastructure integration tests — requires Docker (Testcontainers spins up PostgreSQL)
dotnet test tests/TaskManager.Infrastructure.Tests

# WebApi HTTP contract tests — requires Docker (Testcontainers spins up PostgreSQL)
dotnet test tests/TaskManager.WebApi.Tests
```

> Integration and WebApi tests use **Testcontainers**. Docker must be running before executing them. No manual database setup is needed — each test run spins up its own ephemeral PostgreSQL container.


## Environment Configuration

### Backend — `backend/src/TaskManager.WebApi/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=taskmanager;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Secret": "CHANGE_THIS_TO_A_LONG_RANDOM_SECRET",
    "Issuer": "taskmanager-api",
    "Audience": "taskmanager-client",
    "ExpiryHours": 1
  }
}
```

For local overrides without modifying tracked files, create `appsettings.Development.json` (already in `.gitignore`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=taskmanager;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Secret": "your-local-dev-secret-at-least-32-characters-long"
  }
}
```

### Frontend — `frontend/src/environments/environment.ts`

```typescript
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5000/api'
};
```

Update `apiBaseUrl` if the API runs on a different host or port.

## API Overview

Base URL: `http://localhost:5001/api` (Docker) / `http://localhost:5000/api` (manual run)

All protected endpoints require `Authorization: Bearer <token>`.
All error responses follow **RFC 7807 Problem Details** (`application/problem+json`).

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `POST` | `/auth/register` | No | Register a new user |
| `POST` | `/auth/login` | No | Login and receive a JWT |
| `GET` | `/auth/health` | No | Health check |
| `GET` | `/auth/me` | Yes | Get current user info |
| `GET` | `/tasks?page=1&pageSize=10` | Yes | Paginated task list |
| `GET` | `/tasks/{id}` | Yes | Get task by ID |
| `POST` | `/tasks` | Yes | Create a task |
| `PUT` | `/tasks/{id}` | Yes | Update a task |
| `DELETE` | `/tasks/{id}` | Yes | Delete a task |
| `POST` | `/demo/seed` | Yes | Seed demo users and tasks |

### Pagination response shape

```json
{
  "items": [...],
  "totalCount": 47,
  "page": 2,
  "pageSize": 10,
  "totalPages": 5
}
```

### Error response shape (RFC 7807)

```json
{
  "type": "not_found",
  "title": "Resource Not Found",
  "status": 404,
  "detail": "Task with ID '...' was not found.",
  "instance": "/api/tasks/...",
  "traceId": "00-abc123-def456-00"
}
```

---

## SDK Version Pin

`global.json` at the repository root pins the exact .NET SDK version:

```json
{
  "sdk": {
    "version": "9.0.314",
    "rollForward": "disable"
  }
}
```

This ensures consistent builds across all developer machines and CI pipelines regardless of what SDK versions are installed globally.
