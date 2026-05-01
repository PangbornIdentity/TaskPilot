# TaskPilot

A personal task management web app with a REST API for LLM clients (ChatGPT, Claude, Copilot, etc.).

Built on .NET 10, Blazor WebAssembly, ASP.NET Core, Entity Framework Core, and MudBlazor.

---

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows / macOS / Linux

### 1. Clone and restore

```bash
git clone <repo-url>
cd TaskPilot
dotnet restore
```

### 2. Set the HMAC secret

The server requires an HMAC secret for API key hashing. Set it via `dotnet user-secrets`:

```bash
cd src/TaskPilot.Server
dotnet user-secrets set "Hmac:SecretKey" "$(openssl rand -base64 32)"
```

On Windows (PowerShell):

```powershell
cd src/TaskPilot.Server
$key = [Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
dotnet user-secrets set "Hmac:SecretKey" $key
```

### 3. Run

```bash
dotnet run --project src
```

The app will:
- Create `taskpilot.db` (SQLite) on first run
- Apply the schema automatically (`EnsureCreated`)
- Serve the app at `http://localhost:5125`
- Serve the API at `http://localhost:5125/api/v1/`
- Serve Swagger UI at `http://localhost:5125/swagger` (development only)

### 4. First use

1. Open `https://localhost:5001`
2. Click **Register** to create your account
3. Start adding tasks

---

## Running Tests

```bash
# All tests
dotnet test

# Unit tests only
dotnet test tests/TaskPilot.Tests.Unit

# Integration tests only
dotnet test tests/TaskPilot.Tests.Integration

# With detailed output
dotnet test --logger "console;verbosity=normal"
```

**Test coverage:** 250+ tests (159 unit + 91 integration + a Playwright E2E suite that runs against `localhost:5125`). Smoke tests under `Smoke/DeploymentSmokeTests.cs` require a running server and are skipped by default.

---

## REST API

Base URL: `/api/v1/`
Authentication: `X-Api-Key: <your-key>` header

### Generate an API key

1. Log in to the web UI
2. Go to **Settings → API Keys**
3. Enter a label (e.g., "ChatGPT-Work") and click **Generate**
4. Copy the key — it is shown **once only**

### Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/tasks` | List tasks (filterable, pageable) |
| `GET` | `/tasks/{id}` | Get a single task |
| `POST` | `/tasks` | Create a task |
| `PUT` | `/tasks/{id}` | Full update |
| `PATCH` | `/tasks/{id}` | Partial update |
| `DELETE` | `/tasks/{id}` | Soft-delete |
| `POST` | `/tasks/{id}/complete` | Mark complete (optional result analysis) |
| `GET` | `/tasks/stats` | Aggregated statistics |
| `GET` | `/tags` | List tags (response includes `taskCount` per tag) |
| `POST` | `/tags` | Create a tag |
| `PUT` | `/tags/{id}` | Rename / recolor a tag (returns 409 on duplicate name within the same user) |
| `DELETE` | `/tags/{id}` | Delete a tag |

### Query parameters for `GET /tasks`

| Param | Type | Description |
|-------|------|-------------|
| `status` | enum | `NotStarted`, `InProgress`, `Blocked`, `Completed`, `Cancelled` |
| `type` | string | `Work`, `Personal`, `Health`, `Finance`, `Learning`, `Other` |
| `priority` | enum | `Critical`, `High`, `Medium`, `Low` |
| `search` | string | Full-text search on title + description |
| `tags` | string | Comma-separated tag names |
| `isRecurring` | bool | Filter recurring tasks only |
| `includeOnlyIncomplete` | bool | When `true`, returns only `NotStarted`/`InProgress`/`Blocked` (excludes `Completed` and `Cancelled`). Default sort becomes priority asc (Critical first), then targetDate asc nulls-last. |
| `overdueOnly` | bool | When `true`, returns only tasks with `targetDate < UtcNow`, `targetDate != null`, AND incomplete status. Composes with all other filters. |
| `page` | int | Page number (default: 1) |
| `pageSize` | int | Results per page (default: 20) |
| `sortBy` | string | `title`, `area`, `type`, `priority`, `status`, `targetDate`, `createdDate`, `lastModifiedDate`. The first six are exposed via clickable column headers in the web UI's list view; the last two are reachable via the desktop `Sort▼` menu. |
| `sortDir` | string | `asc`, `desc` |

> **v1.11 note**: the web UI uses `?incomplete=true` as a shorter page-URL alias for the API's `?includeOnlyIncomplete=true`. They map to the same repository filter. API consumers should keep using `includeOnlyIncomplete` — it matches the C# property name and the existing integration-test contract.

### Response envelope

All responses use a standard envelope:

```json
// Single resource
{ "data": { ... }, "meta": { "timestamp": "...", "requestId": "..." } }

// List
{ "data": [...], "meta": { "timestamp": "...", "requestId": "...", "page": 1, "pageSize": 20, "totalCount": 42, "totalPages": 3 } }

// Error
{ "error": { "code": "VALIDATION_ERROR", "message": "...", "details": [{ "field": "title", "message": "..." }] } }
```

### Example: Create a task

```bash
curl -X POST https://localhost:5001/api/v1/tasks \
  -H "X-Api-Key: <your-key>" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Review Q2 report",
    "type": "Work",
    "priority": 2,
    "status": 0,
    "targetDateType": 0,
    "targetDate": "2026-04-01T00:00:00Z",
    "isRecurring": false
  }'
```

---

## Project Structure

```
TaskPilot/
├── src/
│   ├── TaskPilot.Server/        ASP.NET Core Web API host + Blazor WASM host
│   │   ├── Auth/                ApiKeyAuthenticationHandler
│   │   ├── Controllers/         Tasks, Tags, ApiKeys, Audit, Account
│   │   ├── Data/                ApplicationDbContext, EF configurations
│   │   ├── Entities/            TaskItem, Tag, ApiKey, ApiAuditLog, TaskActivityLog
│   │   ├── Extensions/          DI registration, middleware registration
│   │   ├── Middleware/          ApiAuditMiddleware, GlobalExceptionMiddleware
│   │   ├── Repositories/        Generic + specialized repositories
│   │   └── Services/            Task, Tag, ApiKey, Stats, Audit services
│   ├── TaskPilot.Client/        Blazor WebAssembly
│   │   ├── Components/          Reusable UI components
│   │   ├── Pages/               Dashboard, Tasks, TaskDetail, Audit, Settings
│   │   ├── Services/            HTTP clients, auth, toast, theme
│   │   └── wwwroot/             Static assets
│   └── TaskPilot.Shared/        DTOs, enums, validators (shared by Server + Client)
│       ├── Constants/
│       ├── DTOs/
│       ├── Enums/
│       └── Validators/
└── tests/
    ├── TaskPilot.Tests.Unit/        xUnit + Moq + bUnit
    ├── TaskPilot.Tests.Integration/ xUnit + WebApplicationFactory
    └── TaskPilot.Tests.E2E/         Playwright for .NET (iteration 2)
```

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Runtime | .NET 10, C# 13 |
| Frontend | Blazor WebAssembly (hosted) |
| UI Library | MudBlazor 9.2.0 |
| Charts | Blazor-ApexCharts 6.1.0 |
| Backend | ASP.NET Core 10 Web API |
| Auth | ASP.NET Core Identity (cookie) + custom API key handler (HMAC-SHA256) |
| ORM | Entity Framework Core 10 |
| Database | SQLite (iteration 1) |
| Validation | FluentValidation |
| Logging | Serilog |
| Testing | xUnit + Moq + bUnit + WebApplicationFactory |

---

## Configuration

`appsettings.json` (safe defaults — no secrets):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=taskpilot.db"
  },
  "Hmac": {
    "SecretKey": ""
  }
}
```

The `Hmac:SecretKey` **must** be set via user-secrets locally or environment variable in production. The app will throw `InvalidOperationException` on startup if it is empty.

---

## Deployment

### Database Migrations

TaskPilot uses a two-path schema strategy:

- **Development (SQLite):** Schema is created automatically via `EnsureCreatedAsync()` on startup. No migrations are applied. Changes to the model are picked up immediately after restart.
- **Production (Azure SQL):** `MigrateAsync()` runs on startup and applies all pending migrations automatically.

#### Adding a new migration (for contributors)

1. Make your entity/configuration changes in `src/`
2. Run from the solution root:
   ```bash
   dotnet ef migrations add YourMigrationName --project src
   ```
   The `DesignTimeDbContextFactory` in `src/Data/` forces SQL Server provider, ensuring Azure SQL-compatible types (`uniqueidentifier`, `nvarchar`, `datetime2`, `bit`).
3. Open the generated migration file and verify SQL Server types (not SQLite types)
4. If the migration includes seed data, use `migrationBuilder.InsertData()` in `Up()` and `migrationBuilder.DeleteData()` in `Down()`

#### Deploying to Azure

After any migration change, the standard manual deployment process is:
```bash
# 1. Publish
dotnet publish src/TaskPilot.csproj -c Release -o ./publish --nologo

# 2. Create Linux-compatible zip (forward-slash paths)
python -c "
import zipfile, os
with zipfile.ZipFile('taskpilot-deploy.zip', 'w', zipfile.ZIP_DEFLATED) as zf:
    for root, dirs, files in os.walk('./publish'):
        for file in files:
            abs_path = os.path.join(root, file)
            arc_name = os.path.relpath(abs_path, './publish').replace(os.sep, '/')
            zf.write(abs_path, arc_name)
"

# 3. Deploy
az webapp deploy --name taskpilot --resource-group taskpilot-rg --src-path ./taskpilot-deploy.zip --type zip
```

`MigrateAsync()` runs automatically on first request after deploy, applying any new migrations to Azure SQL.

---

## Iteration 2 Roadmap

The following are planned for iteration 2 (no code changes required — configuration only):

| Feature | Notes |
|---------|-------|
| Azure App Service | Replace `dotnet run` with hosted deployment |
| Azure SQL / PostgreSQL | Replace SQLite connection string |
| Azure Key Vault | Replace `dotnet user-secrets` for `Hmac:SecretKey` |
| Application Insights | Add Serilog sink |
| GitHub Actions CI/CD | Build → test → deploy pipeline |
| Per-API-key rate limiting | Insertion point documented in ARCHITECTURE.md §4.8 |
| Playwright E2E tests | `tests/TaskPilot.Tests.E2E/` scaffolded and ready |

---

## Security Notes

- API keys are hashed with **HMAC-SHA256** before storage. The plaintext key is shown once on generation and never stored.
- All task deletes are **soft deletes** — `IsDeleted = true` + `DeletedAt`. Data is never hard-deleted via the UI or API.
- Every API key request is logged to `ApiAuditLog` (request body hashed with SHA256 — full body never stored).
- Every task field change is logged to `TaskActivityLog`.
- See `SECURITY-VALIDATION.md` for the full security checklist (37/37 checks pass).
