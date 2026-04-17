# TaskPilot Changelog

> All changes, decisions, and deviations are logged here in reverse chronological order.
> Format: `[YYYY-MM-DD] TYPE: Description — Files affected`
>
> **Types:** Feature | Fix | Design | Architecture | Refactor | Security | Test | Docs | Config

---

## 2026-04-17 — Bundle CDN assets locally, fix uptime bug (v1.9)

### Fix | CDN assets bundled locally to avoid Tracking Prevention breakage

- **Root cause**: Edge/Brave Tracking Prevention blocks `cdn.jsdelivr.net` as a third-party tracker, breaking Bootstrap CSS/JS, Bootstrap Icons, htmx, and ApexCharts on Azure
- **Fix**: Downloaded all CDN assets to `src/wwwroot/lib/` and updated all layout/page references to use local paths
- **Files added**: `lib/bootstrap/css/bootstrap.min.css`, `lib/bootstrap/js/bootstrap.bundle.min.js`, `lib/bootstrap-icons/font/bootstrap-icons.css`, `lib/bootstrap-icons/font/fonts/bootstrap-icons.woff2`, `lib/bootstrap-icons/font/fonts/bootstrap-icons.woff`, `lib/htmx/htmx.min.js`, `lib/apexcharts/apexcharts.min.js`
- **Files changed**: `src/Pages/Shared/_Layout.cshtml`, `src/Pages/Shared/_LoginLayout.cshtml`, `src/Pages/Health/Index.cshtml`, `src/Pages/Index.cshtml`

### Fix | Negative uptime in health endpoint

- **Root cause**: `ProcessUptime.cs` used `DateTime.UtcNow` in a static field initializer, which could race with the first health check call
- **Fix**: Changed to `Process.GetCurrentProcess().StartTime.ToUniversalTime()` for OS-level accuracy
- **File changed**: `src/Diagnostics/ProcessUptime.cs`

### Config | .gitignore update

- Added `publish-linux/` to `.gitignore` to exclude Azure deployment build artifacts

---

## 2026-04-15 — Health & Diagnostics subsystem v1.8 (implementation)

### Feature | Health endpoints, build stamping, public health page, smoke script

- **Assembly version** (`src/TaskPilot.csproj`): `<Version>1.8.0</Version>`; MSBuild `StampGitMetadata` target runs `git rev-parse HEAD/--short HEAD` at build time and emits `AssemblyMetadataAttribute` for `GitCommit`, `GitCommitShort`, `BuildTimestampUtc` — falls back to `"unknown"` when git unavailable; no `Microsoft.SourceLink.GitHub` package added
- **BuildInfo** (`src/Diagnostics/BuildInfo.cs`): static class reading assembly metadata once; `Version`, `GitCommit`, `GitCommitShort`, `BuildTimestampUtc`
- **ProcessUptime** (`src/Diagnostics/ProcessUptime.cs`): captures `DateTime.UtcNow` at static init for uptime calculation
- **Health DTOs** (`src/Models/Health/`): `VersionResponse`, `LivenessResponse`, `HealthResponse`, `HealthCheckResult`, `AssetsResponse` records
- **Health interface + 7 checks** (`src/Services/Health/`): `IHealthCheckComponent`, `HealthStatuses` constants; implementations: `DatabaseHealthCheck` (required, 2s timeout), `MigrationsHealthCheck` (required), `ConfigHealthCheck` (required), `AuthHandlersHealthCheck` (required), `McpHealthCheck` (optional), `TempWritableHealthCheck` (optional), `AssemblyMetadataHealthCheck` (optional)
- **HealthService** (`src/Services/Health/HealthService.cs`): `RunReadinessAsync` (required checks only), `RunFullAsync` (all checks); aggregation: unhealthy if any required fails → 503, degraded if only optional fail → 200
- **AssetsService** (`src/Services/Health/AssetsService.cs`): computes SHA-256 fingerprints for tracked static assets; cached singleton
- **HealthController** (`src/Controllers/HealthController.cs`): endpoints `/api/v1/health/{version,live,ready,full,assets}`; all anonymous; all set `Cache-Control: no-store`, `Pragma: no-cache`, `Expires: 0`, `X-TaskPilot-Version`, `X-TaskPilot-Commit`
- **Public /health page** (`src/Pages/Health/Index.cshtml(.cs)`): anonymous; renders full check result; green/red/amber badge; version block with GitHub commit deep-link; checks table; auto-refreshes every 30s; `.tp-health-status`, `.tp-health-version`, `.tp-health-commit`, `.tp-raw-json-link` CSS class hooks
- **Sidebar version pill** (`src/Pages/Shared/_Layout.cshtml`): rebound from `_changelogService.GetLatest()` to `BuildInfo.Version + BuildInfo.GitCommitShort`; pill now links to `/health`; `app-changelog.json` remains for What's New release notes only
- **Audit exclusion** (`src/Middleware/ApiAuditMiddleware.cs`): `/api/v1/health/*` excluded from audit log writes to prevent Azure health probe noise
- **DI registration** (`src/Extensions/ServiceCollectionExtensions.cs`): `AddTaskPilotHealth()` registers all 7 checks + `IHealthService` + `AssetsService`
- **Program.cs** (`src/Program.cs`): calls `AddTaskPilotHealth()`; adds `AllowAnonymousToPage("/Health/Index")`
- **appsettings.json** (`src/appsettings.json`): `Diagnostics:GitHubRepoUrl` = `https://github.com/PangbornIdentity/TaskPilot`
- **ApiRoutes.cs** (`src/Constants/ApiRoutes.cs`): `ApiRoutes.Health` constants added
- **CSS** (`src/wwwroot/css/app.css`): `.tp-version-pill-container` and `.tp-version-pill-link` styles
- **smoke.ps1** (`scripts/smoke.ps1`): parameterized by `-BaseUrl` and `-ExpectedCommit`; covers HLTH-060 through HLTH-065; same script runs against `localhost:5125` and Azure
- **Unit tests** (`tests/TaskPilot.Tests.Unit/Diagnostics/`): `BuildInfoTests.cs` (HLTH-001–005), `HealthCheckTests.cs` (HLTH-010–024) — 21 new unit tests
- **Integration tests** (`tests/TaskPilot.Tests.Integration/Health/HealthEndpointTests.cs`): HLTH-030–045 — 16 new integration tests
- **Smoke integration tests** (`tests/TaskPilot.Tests.Integration/Smoke/DeploymentSmokeTests.cs`): HLTH-060–065 — 6 smoke tests (skipped when server not running; run with `SMOKE_BASE_URL` env var)
- **E2E tests** (`tests/TaskPilot.Tests.E2E/Health/HealthPageTests.cs`): HLTH-050–057 — 8 E2E tests
- Files affected: `src/TaskPilot.csproj`, `src/Diagnostics/BuildInfo.cs`, `src/Diagnostics/ProcessUptime.cs`, `src/Models/Health/*.cs` (5 files), `src/Services/Health/*.cs` + `Checks/*.cs` (10 files), `src/Controllers/HealthController.cs`, `src/Pages/Health/Index.cshtml(.cs)`, `src/Pages/Shared/_Layout.cshtml`, `src/Middleware/ApiAuditMiddleware.cs`, `src/Extensions/ServiceCollectionExtensions.cs`, `src/Program.cs`, `src/appsettings.json`, `src/Constants/ApiRoutes.cs`, `src/wwwroot/css/app.css`, `src/app-changelog.json`, `scripts/smoke.ps1`, `tests/TaskPilot.Tests.Unit/Diagnostics/BuildInfoTests.cs`, `tests/TaskPilot.Tests.Unit/Diagnostics/HealthCheckTests.cs`, `tests/TaskPilot.Tests.Integration/Health/HealthEndpointTests.cs`, `tests/TaskPilot.Tests.Integration/Smoke/DeploymentSmokeTests.cs`, `tests/TaskPilot.Tests.E2E/Health/HealthPageTests.cs`

---

## 2026-04-15 — Health & Diagnostics subsystem (design only)

### Architecture | Health endpoints, build-time version stamping, public /health page

- **ARCHITECTURE.md**: new Section 12 "Health & Diagnostics" — endpoint map, build-time version injection via MSBuild Exec target stamping `AssemblyMetadataAttribute`, response shapes (`VersionResponse`, `LivenessResponse`, `HealthResponse`, `HealthCheckResult`, `AssetsResponse`), seven `IHealthCheckComponent` implementations (database, migrations, config, auth-handlers, mcp, temp-writable, assembly-metadata), no-cache header policy, CDN/Front Door verification steps, hybrid static-asset fingerprinting (`asp-append-version` + `/health/assets` manifest), public `/health` Razor Page spec, Azure App Service / Application Insights integration table, post-deploy `smoke.ps1` script, local-dev parity statement
- **ARCHITECTURE.md §3.4b**: new endpoint table for `/api/v1/health/{version,live,ready,full,assets}` and `/health` page; TOC updated to include Sections 11 and 12
- **REQUIREMENTS.md §4.9**: new feature entry — sidebar version pill rebinds to `BuildInfo` (not `app-changelog.json`), public `/health` page spec, authoritativeness rule (binary is source of truth, JSON is release notes only)
- **REQUIREMENTS.md §5.3a**: new REST API endpoint table for health endpoints; documents anonymous access, no-cache headers, audit-log exclusion
- **TEST-CASES.md §17**: 36 new test cases (HLTH-001 through HLTH-065) covering BuildInfo unit tests, per-component health check unit tests, WebApplicationFactory integration tests for all four endpoints (envelope shape, 503 semantics, no-cache headers, audit exclusion), Playwright E2E for the public `/health` page and sidebar version pill parity, and parameterized deployment smoke tests for local + Azure
- **Open decisions flagged**: (1) hand-roll `IHealthCheckComponent` vs. `Microsoft.Extensions.Diagnostics.HealthChecks` package — recommend hand-roll; (2) `Microsoft.SourceLink.GitHub` vs. plain MSBuild Exec — recommend Exec; (3) GitHub repo URL config key for commit deep-links; (4) confirm `/api/v1/health/*` exclusion from `ApiKeyAuditMiddleware`
- **No code changes** — implementation is deferred to `fullstack-dev` agent in next phase
- Files affected: `ARCHITECTURE.md`, `REQUIREMENTS.md`, `TEST-CASES.md`

---

## 2026-04-12 — Mobile-responsive layout

### Feature | Responsive sidebar, media queries, table scroll wrappers

- **Sidebar — tablet (641–1024px)** (`src/wwwroot/css/app.css`): collapses to 60px icon-only rail; nav text, user email, version pill, API docs label all hidden; icons centered with 44px touch targets
- **Sidebar — mobile (≤640px)** (`src/wwwroot/css/app.css`): becomes `position: fixed`, slides in from left via `.open` class; starts below the 56px top bar (`top: 56px; height: calc(100% - 56px)`)
- **Mobile top bar** (`src/Pages/Shared/_Layout.cshtml`, `src/wwwroot/css/app.css`): 56px fixed header with hamburger button (≤640px only) — `tp-mobile-header`, `tp-hamburger`, `tp-mobile-brand`
- **Sidebar backdrop** (`src/Pages/Shared/_Layout.cshtml`, `src/wwwroot/css/app.css`): semi-transparent overlay closes sidebar on tap; JS `toggleSidebar()` / `closeSidebar()` functions added, nav links auto-close sidebar on mobile tap
- **Content padding** (`src/wwwroot/css/app.css`): reduced to 24px on tablet, 16px on mobile
- **Stats/charts grids** (`src/wwwroot/css/app.css`): stats → 2-col on mobile; charts/kanban → 1-col on mobile and tablet
- **Table scroll wrappers** (`src/Pages/Index.cshtml`, `src/Pages/Tasks/Index.cshtml`, `src/Pages/Audit/Index.cshtml`, `src/Pages/Integrations/Index.cshtml`): all `tp-table` elements wrapped in `tp-table-scroll` (overflow-x: auto); Integrations MCP tools table wrapped in Bootstrap `table-responsive`
- **Filters/search** (`src/wwwroot/css/app.css`): `.tp-search` min-width removed on mobile, quick-add stacks to full-width on mobile
- **Touch targets** (`src/wwwroot/css/app.css`): nav links, logout button, changelog link all min-height 44px on mobile
- **Changelog** (`src/app-changelog.json`): v1.7 added
- **Mobile E2E tests** (`tests/TaskPilot.Tests.E2E/Mobile/MobileLayoutTests.cs`): 10 new Playwright tests (MOB-001 to MOB-010) covering hamburger visibility, sidebar open/close, backdrop dismiss, tablet icon rail, and changelog v1.7 presence
- **TEST-CASES.md**: Section 16 added with mobile test case table
- Files affected: `src/wwwroot/css/app.css`, `src/Pages/Shared/_Layout.cshtml`, `src/Pages/Index.cshtml`, `src/Pages/Tasks/Index.cshtml`, `src/Pages/Audit/Index.cshtml`, `src/Pages/Integrations/Index.cshtml`, `src/app-changelog.json`, `tests/TaskPilot.Tests.E2E/Mobile/MobileLayoutTests.cs`, `TEST-CASES.md`

---

## 2026-04-05 — MCP Server (Model Context Protocol)

### Feature | Remote MCP server at /mcp with 9 tools, API key auth

- **NuGet packages** (`src/TaskPilot.csproj`): `ModelContextProtocol` 1.2.0 + `ModelContextProtocol.AspNetCore` 1.2.0
- **MCP tools** (`src/Mcp/TaskPilotMcpTools.cs`): new class with 9 tools — `list_tasks`, `get_task`, `create_task`, `update_task`, `complete_task`, `delete_task`, `get_stats`, `list_tags`, `list_task_types` — calls existing services directly
- **Auth policy** (`src/Program.cs`): added `"McpApiKey"` named policy (ApiKeyScheme only); `MapMcp("/mcp").RequireAuthorization("McpApiKey")`
- **Service registration** (`src/Extensions/ServiceCollectionExtensions.cs`): `AddTaskPilotMcp()` extension method — registers `AddHttpContextAccessor`, `AddMcpServer().WithHttpTransport().WithTools<TaskPilotMcpTools>()`
- **Integrations page** (`src/Pages/Integrations/Index.cshtml`): MCP section updated from "Coming Soon" to live — shows endpoint URL, Claude Desktop config block with copy button, tools table, transport note
- **Unit tests** (`tests/TaskPilot.Tests.Unit/Mcp/TaskPilotMcpToolsTests.cs`): 14 new tests (MCP-001 to MCP-014)
- Files affected: `src/TaskPilot.csproj`, `src/Program.cs`, `src/Extensions/ServiceCollectionExtensions.cs`, `src/Mcp/TaskPilotMcpTools.cs`, `src/Pages/Integrations/Index.cshtml`, `tests/TaskPilot.Tests.Unit/Mcp/TaskPilotMcpToolsTests.cs`

---

## 2026-04-05 — Fix incorrect stats endpoint path in Integrations page docs

### Fix | Integrations page listed /api/v1/stats — correct path is /api/v1/tasks/stats

- **Bug**: The REST API Reference table on `/integrations` showed `GET /api/v1/stats`, which is a 404. The actual endpoint is `GET /api/v1/tasks/stats` (nested under the tasks controller).
- **Fix** (`src/Pages/Integrations/Index.cshtml`): corrected the path in the endpoint table.
- Files affected: `src/Pages/Integrations/Index.cshtml`

---

## 2026-04-03 — Dashboard Completed-Today Fix

### Fix | CreateTaskAsync and UpdateTaskAsync now set CompletedDate when status is Completed

- **Bug**: Creating a task with Status = Completed left `CompletedDate` null. The dashboard "Completed Today" card queries `CompletedDate`, so these tasks never appeared in the count.
- **Root cause**: `CreateTaskAsync`, `UpdateTaskAsync` (PUT), and `PatchTaskAsync` (PATCH) only set `CompletedDate` via `CompleteTaskAsync`. Setting status = Completed by any other path left it null.
- **Fix** (`src/Services/TaskService.cs`): all three methods now set `CompletedDate = DateTime.UtcNow` when status transitions to Completed and `CompletedDate` is not already set.
- **Unit tests** (`tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs`): 2 new tests — `CreateTaskAsync_WithCompletedStatus_SetsCompletedDate` and `CreateTaskAsync_WithNonCompletedStatus_CompletedDateIsNull`
- **In-app changelog** (`src/app-changelog.json`): v1.5 entry added
- Files affected: `src/Services/TaskService.cs`, `tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs`, `src/app-changelog.json`

---

## 2026-04-01 — LLM Integrations Page and Swagger UI Link

### Feature | Integrations page, API Docs link, and MCP coming-soon placeholder

- **Integrations page** (`src/Pages/Integrations/Index.cshtml`, `Index.cshtml.cs`): new `/integrations` route with 5 sections — Quick Start (curl example with copy button), REST API endpoint table, Claude tool definitions (list_tasks + create_task), OpenAI function definitions, MCP coming-soon placeholder
- **Swagger nav link** (`src/Pages/Shared/_Layout.cshtml`): "API Docs" link added to sidebar (dev mode only, opens `/swagger` in new tab)
- **Integrations nav item** (`src/Pages/Shared/_Layout.cshtml`): "Integrations" added to sidebar nav with plug icon
- **Settings — API Reference card** (`src/Pages/Settings/Index.cshtml`, `Index.cshtml.cs`): new section linking to Integrations page and Swagger (dev mode only)
- **CSS** (`src/wwwroot/css/app.css`): `.tp-code-block`, `.tp-step-number`, `.tp-swagger-nav` styles added
- **In-app changelog** (`src/app-changelog.json`): v1.4 entry added — visible at `/changelog`
- **E2E tests** (`tests/TaskPilot.Tests.E2E/Integrations/IntegrationsPageTests.cs`): 10 new tests
- Files affected: `src/Pages/Integrations/Index.cshtml`, `src/Pages/Integrations/Index.cshtml.cs`, `src/Pages/Shared/_Layout.cshtml`, `src/Pages/Settings/Index.cshtml`, `src/Pages/Settings/Index.cshtml.cs`, `src/wwwroot/css/app.css`, `src/app-changelog.json`, `tests/TaskPilot.Tests.E2E/Integrations/IntegrationsPageTests.cs`, `ARCHITECTURE.md`, `REQUIREMENTS.md`, `TEST-CASES.md`

---

## 2026-03-31 — Tags, Task Type, and Area — Test Suite Expansion

### Test | Unit, integration, and E2E tests added for Area, TaskType, and Tags features

- **Unit — TaskService** (`tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs`): +7 tests (U-T-024 to U-T-030) covering `Area` persistence on create/update/patch and `TaskTypeId` persistence on create/patch
- **Unit — TaskTypeService** (`tests/TaskPilot.Tests.Unit/Services/TaskTypeServiceTests.cs`): new file with 2 tests (U-TT-001 to U-TT-002) — active type list ordering and empty repository edge case; uses mocked `ITaskTypeRepository`
- **Unit — StatsService** (`tests/TaskPilot.Tests.Unit/Services/StatsServiceTests.cs`): +2 tests (U-ST-006 to U-ST-007) — `CompletionsByArea` personal/work split and `TopTags` top-5 by task count
- **Unit — Infrastructure** (`tests/TaskPilot.Tests.Unit/Helpers/TestDbContextFactory.cs`): new helper that works around EF10 `HasDefaultValue(0)` breaking change on enum properties by building a clean IModel via a plain `DbContext` subclass and injecting it via `UseModel()`; all 11 previously-failing `StatsServiceTests` now pass
- **Integration — WebAppFactory** (`tests/TaskPilot.Tests.Integration/WebAppFactory.cs`): added inner `PatchedApplicationDbContext` that manually configures Identity + TaskPilot entities without `HasDefaultValue`, fixing all 55 previously-failing integration tests
- **Integration — TaskTypeApiTests** (`tests/TaskPilot.Tests.Integration/TaskTypes/TaskTypeApiTests.cs`): new file with 3 tests (I-TT-001 to I-TT-003) — unauthenticated returns 401, seeded list with 6 types returned in sort order, all types have id and name
- **Integration — TasksApiTests** (`tests/TaskPilot.Tests.Integration/Tasks/TasksApiTests.cs`): +6 tests (I-T-036 to I-T-041) — area persistence, task type name in response, tag IDs in response, and filter-by-area/taskTypeId/tagIds
- **E2E** (`tests/TaskPilot.Tests.E2E/Tasks/TaskAreaTypeTagTests.cs`): new file with 5 tests (E-ATT-001 to E-ATT-005) — area filter, all-filter, default Personal area, type on card, tag pill; uses graceful fallback assertions
- **TEST-CASES.md**: added Section 14 documenting all 20 new test cases
- **Test counts (before → after):** Unit 81 → 92 (all passing), Integration 55 → 64 (all passing; previously 0 passing), E2E +5 new tests
- Files affected: `tests/TaskPilot.Tests.Unit/Helpers/TestDbContextFactory.cs`, `tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs`, `tests/TaskPilot.Tests.Unit/Services/TaskTypeServiceTests.cs`, `tests/TaskPilot.Tests.Unit/Services/StatsServiceTests.cs`, `tests/TaskPilot.Tests.Integration/WebAppFactory.cs`, `tests/TaskPilot.Tests.Integration/TaskTypes/TaskTypeApiTests.cs`, `tests/TaskPilot.Tests.Integration/Tasks/TasksApiTests.cs`, `tests/TaskPilot.Tests.E2E/Tasks/TaskAreaTypeTagTests.cs`, `TEST-CASES.md`, `CHANGELOG.md`

---

## 2026-03-31 — Tags, TaskType, and Area — Requirements and README Documentation

### Docs | REQUIREMENTS.md and README.md updated to reflect Tags UI, TaskType lookup, and Area feature

**REQUIREMENTS.md:**
- §3.1 Task: removed free-form `Type` field; replaced with `TaskTypeId` (int?, FK to TaskType) and `Area` (enum: Personal=0 / Work=1, default Personal, required)
- §3.2 Tag / §3.3 TaskTag: added note that Tag and TaskTag are now fully UI-exposed (coloured pills on cards, detail, and create/edit form)
- §3.7 TaskType: new lookup table spec — Id, Name, SortOrder, IsActive — with seed data (Task, Goal, Habit, Meeting, Note, Event)
- §4.1 Dashboard: added three new chart specs — completions by Area, top 5 tags by task count, task count by type
- §4.2 Task List View: updated Filters entry to include Area (Personal/Work/All), TaskType (single-select dropdown), and Tags (multi-select, AND logic)
- §4.3 Task Create/Edit: added Area required toggle (default Personal) and TaskType optional dropdown sourced from /api/v1/task-types; tags shown as coloured pills
- §4.4 Task Detail View: added tags shown as coloured pills beneath title/description
- §5.1 Task Endpoints: updated GET /tasks query params (replaced `type` with `taskTypeId` and `area`)
- §5.2 TaskType Endpoints: new section — GET /api/v1/task-types (read-only in iteration 1)
- §5.3 Tag Endpoints: renumbered from §5.2
- §5.4 Response Envelope / §5.5 API Behaviors: renumbered from §5.3/§5.4

**README.md:**
- Added "Deployment" section with "Database Migrations" sub-heading covering: two-path schema strategy (SQLite EnsureCreatedAsync vs Azure SQL MigrateAsync), contributor migration workflow with `dotnet ef migrations add`, and Azure deployment commands (publish → zip → `az webapp deploy`)

Files affected: `REQUIREMENTS.md`, `README.md`, `CHANGELOG.md`

---

## 2026-03-31 — Tags UI, TaskType Lookup, and Area Feature — UX Documentation

### Design | Tag Pill, Area Segmented Control, and TaskType Dropdown component specs; updated wireframes; two new user flows

**DESIGN-SYSTEM.md:**
- Added §9 "New Feature Components" (sections 9.1–9.3); renumbered old §9 → §10, old §10 → §11; updated Table of Contents
- §9.1 Tag Pill: three variants (Display, Removable, Add-new trigger) with full spec — dimensions, typography (`label-sm`, 12px, 500 weight), padding (4px 8px), border-radius (`--radius-full`), 8-swatch colour palette with hex values and light/dark text pairings; tag multi-select dropdown spec including search, inline create, colour picker, keyboard/ARIA requirements
- §9.2 Area Segmented Control: two-segment toggle (Personal / Work); active/inactive/hover/disabled state token table; full-width mobile, fit-content desktop; ARIA `role="group"` / `role="radio"` / keyboard nav spec; usage in form (first field, default Personal) and task list filter bar (both-segments-inactive = no filter)
- §9.3 TaskType Dropdown: Bootstrap `<select class="form-select">`; placeholder "Select type…" (null = optional); options from `GET /api/v1/task-types`; `sm` (32px) in filter bar, `md` (40px) in form; ARIA label spec
- §7 Iconography: added `bi-x`, `bi-plus`, `bi-layout-text-sidebar-reverse`, `bi-person`, `bi-briefcase` icon assignments

**WIREFRAMES.md:**
- Page 2 (Dashboard): added Area Split stat block (new full-width row between summary cards and charts; two stat blocks for Personal/Work completed counts with `bi-person` / `bi-briefcase` icons); added Charts Row 4 with Top Tags (horizontal bar chart, ApexCharts, top 5 tags by task count, `StatsResponse.TopTags`) and Type Breakdown (horizontal bar chart, `StatsResponse.ByType`); empty state spec for Top Tags when no tags exist; updated tablet and mobile layout notes
- Page 3 (Task List — List View): complete rewrite of desktop layout; Area segmented control added as topmost element above filter bar; filter bar updated with Type dropdown and Tags multi-select filter; task row updated to two-line layout showing Area badge + TaskType label (line 1) and tag pills + date (line 2); row height updated to 56px; added filter chip anatomy; AND tag filter logic noted; tablet and mobile layout updated
- Page 4 (Board/Kanban): card spec updated to include Area badge (top-right) and tag pills (Display variant, up to 3 + overflow)
- Page 5 (Task Create/Edit): new field order documented (Area → Type → Title → Description → Priority → Status → Target Date → Tags → Recurring → Result Analysis); Area segmented control as first field; Type dropdown as second field; Tags field layout with removable pills and Add-new trigger; legacy free-text Type pill buttons removed; tablet and mobile slide-over notes added
- Page 6 (Task Detail): metadata grid updated to include Area badge row and TaskType name row; Tags row updated to show Display pill variants

**USER-FLOWS.md:**
- Added Flow 13: "Create a Task with Area, Type, and Tags" — 19 steps covering area selection, type dropdown, tag selection (existing tag), inline tag create with colour picker, form submit, and expected task list appearance; 4 edge cases
- Added Flow 14: "Filter Task List by Area, Type, and Tags" — 12 steps covering progressive filter application (area → type → tags), URL param updates, results count, filter chip appearance, clearing individual filters, and "Clear all filters"; URL parameter mapping table; AND-logic tag filtering decision documented; 4 edge cases

**Design decisions:**
- Tag filter logic is **AND** (tasks must match ALL selected tags). Rationale: narrowing by multiple tags is the primary use case; OR logic would make results less predictable and harder to scan. OR mode deferred to a future iteration.
- Area segmented control supports a **both-inactive** state on the task list (= no area filter, show all tasks). On the create/edit form, one segment is always active (default: Personal) because Area is a required field on the task entity.
- Files affected: `DESIGN-SYSTEM.md`, `WIREFRAMES.md`, `USER-FLOWS.md`, `CHANGELOG.md`

---

## 2026-03-31 — Tags UI, TaskType Lookup, and Area Feature — Architecture Documentation

### Architecture | Document TaskType entity, Area enum, new API endpoints, migration plan, and updated DTOs
- **§1 Solution Structure**: added `TaskType.cs` entity, `TaskTypes/TaskTypeResponse.cs` DTO, `Area.cs` enum to directory tree
- **§2 ER Diagram**: added `TaskType` entity (Id, Name, SortOrder, IsActive); added `Area` (int) and `TaskTypeId` (int FK, nullable) to `TaskItem`; added `TaskType ||--o{ TaskItem : "categorises"` relationship; added comments noting Tag/TaskTag are now UI-exposed
- **§3 API Endpoints**: added §3.1a `GET /api/v1/task-types` endpoint with `TaskTypeResponse` DTO and seed data table; updated `CreateTaskRequest`, `UpdateTaskRequest`, and `PatchTaskRequest` DTOs to include `taskTypeId` (int?, optional), `area` (Area enum, default Personal), `tagIds` (List<Guid>); updated `TaskResponse` DTO to include `taskTypeId`, `taskTypeName`, `area`, `areaName`, and `tags`; added `StatsResponse` DTO spec with `CompletionsByArea` and `TopTags` fields
- **§5 Coding Standards**: added §5.8 documenting that `Area` enum must always serialise as integer (no `StringEnumConverter`); documented `Area` enum values (Personal=0, Work=1) and design rationale for fixed enum over lookup table
- **§7 Azure Migration Map**: added §7.1 migration plan for `AddTaskTypeAreaAndTagsUI` — migration command, `DesignTimeDbContextFactory` note, nullable `TaskTypeId`, `Area` default 0, seed data via `InsertData`, `MigrateAsync` production apply, integration test unaffected note
- **§9 Package Decisions**: added note confirming no new NuGet packages required for this feature
- Files affected: `ARCHITECTURE.md`, `CHANGELOG.md`

---

## 2026-03-27 — Azure Deployment Readiness

### Architecture | Azure SQL provider, EF Core migrations, and production configuration
- Added `Microsoft.EntityFrameworkCore.SqlServer` (v10.0.5) — Azure SQL provider for production
- Added `DesignTimeDbContextFactory` — forces SQL Server provider during `dotnet ef migrations add` so generated migrations use Azure SQL-compatible types (`uniqueidentifier`, `nvarchar`, `datetime2`)
- Generated `InitialCreate` SQL Server migration (`src/Migrations/20260331125530_InitialCreate.cs`) — full schema including all tables, Identity, and indexes
- `Program.cs` startup schema management: `EnsureCreatedAsync` (Development/SQLite), `MigrateAsync` (Production/Azure SQL)
- Added `appsettings.Production.json` — placeholder Azure SQL connection string, Console-only Serilog (no file sink, App Service filesystem is ephemeral), reduced EF Core log verbosity
- Integration tests unaffected — `WebAppFactory` always forces SQLite + Development environment, bypassing `MigrateAsync`
- **Docs updated**: ARCHITECTURE.md §7 (Azure Migration Map — database and logging rows marked implemented), §9 (package list updated with SQL Server entry)
- Files affected: `src/TaskPilot.csproj`, `src/Data/DesignTimeDbContextFactory.cs`, `src/Migrations/20260331125530_InitialCreate.cs`, `src/Extensions/ServiceCollectionExtensions.cs`, `src/Program.cs`, `src/appsettings.Production.json`, `ARCHITECTURE.md`

---

## 2026-03-27 — Changelog Feature

### Feature | In-app changelog page with version history and left-nav entry
- New `/changelog` page shows all versions newest-first with major/minor distinction and change type badges (Feature, Fix, Improvement, Security)
- `app-changelog.json` in project root — no DB, Azure-safe, read once at startup by a singleton `ChangelogService`
- Left sidebar shows "What's new" link with a version pill badge (`v1.2`) above the footer
- Version numbering: `MAJOR.MINOR` — major versions highlighted with purple left-border
- `ChangelogService` accepts JSON content string — fully testable without filesystem mocks
- **Tests**: 10 unit (`ChangelogServiceTests`), 4 integration (`ChangelogPageTests`), 6 E2E (`ChangelogTests`) — 86 unit, 55 integration, 34 E2E, all passing
- Files affected: `src/app-changelog.json`, `src/Models/Changelog/ChangelogModels.cs`, `src/Services/Interfaces/IChangelogService.cs`, `src/Services/ChangelogService.cs`, `src/Extensions/ServiceCollectionExtensions.cs`, `src/Program.cs`, `src/Pages/Changelog/Index.cshtml(.cs)`, `src/Pages/Shared/_Layout.cshtml`, `src/wwwroot/css/app.css`

---

## 2026-03-27 — Activity Log CRUD Tests Added

### Test | Unit and integration tests updated and passing for full CRUD activity logging
- **Unit** (76 passing, +5): `CreateTaskAsync_WritesCreatedActivityLog`, `DeleteTaskAsync_WritesDeletedActivityLog`, `CompleteTaskAsync_LogsCorrectOldStatus`, `PatchTaskAsync_WritesPerFieldActivityLogs`, `PatchTaskAsync_UnchangedFields_DoNotProduceActivityLogs`; fixed `DeleteTaskAsync` mock from `GetByIdAsync` → `GetByIdWithTagsAsync`
- **Integration** (51 passing, +2): `GetActivityLogs_AfterTaskCreate_ContainsCreatedLog`, `GetActivityLogs_AfterTaskDelete_DeletedLogStillVisible`
- Files affected: `tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs`, `tests/TaskPilot.Tests.Integration/ActivityLogs/ActivityLogApiTests.cs`, `TEST-CASES.md`

---

## 2026-03-27 — Activity Log Full CRUD Coverage

### Fix | Activity log now records all CRUD operations, not just field updates
- **Create**: new `"Created"` log entry written when a task is created (NewValue = task title)
- **Delete**: new `"Deleted"` log entry written before soft-delete (OldValue = task title); repository now uses `IgnoreQueryFilters()` on the Tasks join so logs for deleted tasks remain visible in the audit trail
- **Complete**: fixed bug where `OldValue` was read after `task.Status` was already set to `Completed`, causing it to log `Completed → Completed`; status is now captured before mutation
- **Patch**: replaced single generic `"patch"` log entry with per-field change entries matching `UpdateTask` behavior
- Files affected: `src/Services/TaskService.cs`, `src/Repositories/ActivityLogRepository.cs`

---

## 2026-03-26 — E2E Test Coverage for Activity Log UI

### Test | Added E2E tests covering Change History and Task History tab with data
- `TaskDetail_AfterEdit_ShowsChangeHistory` — creates a task, edits it via the detail form, verifies "Change History" section renders
- `AuditPage_TaskHistoryTab_AfterTaskEdit_ShowsLog` — creates and edits a task then verifies the Task History tab on `/audit` shows log entries
- Files affected: `tests/TaskPilot.Tests.E2E/Tasks/TaskLifecycleTests.cs`, `tests/TaskPilot.Tests.E2E/Audit/AuditTests.cs`, `TEST-CASES.md`

---

## 2026-03-27 — Unified Audit Area + Activity Log API

### Feature | Combined task history and API access into a single tabbed audit area
- **New `GET /api/v1/activity-logs`** read-only endpoint — paginated, filterable by taskId, from, to, fieldChanged, changedBy
- **`/audit` page** redesigned with Bootstrap nav-tabs: "Task History" (default) + "API Access"
  - Task History tab: shows all task field changes across all tasks, filterable, links to task detail
  - API Access tab: existing API key request log (moved into tab)
- **Task detail page** (`/tasks/{id}`) now shows a "Change History" table at the bottom with full per-task log
- **New service/repo layer**: `IActivityLogService` + `ActivityLogService`, `IActivityLogRepository` + `ActivityLogRepository`
- **New tests**: `ActivityLogServiceTests.cs` (6 unit), `ActivityLogApiTests.cs` (6 integration), updated `AuditTests.cs` E2E (5 tests)
- Files affected: `src/Models/Audit/ActivityLogModels.cs`, `src/Repositories/Interfaces/IActivityLogRepository.cs`, `src/Repositories/ActivityLogRepository.cs`, `src/Services/Interfaces/IActivityLogService.cs`, `src/Services/ActivityLogService.cs`, `src/Controllers/ActivityLogController.cs`, `src/Extensions/ServiceCollectionExtensions.cs`, `src/Pages/Audit/Index.cshtml`, `src/Pages/Audit/Index.cshtml.cs`, `src/Pages/Tasks/Detail.cshtml`, `src/Pages/Tasks/Detail.cshtml.cs`

---

## 2026-03-26 — Test Coverage Expansion

### Test | Added missing unit and integration tests to fill pyramid gaps
- Added `AuditServiceTests.cs` (5 tests) — mocks `IAuditLogRepository`, verifies mapping/delegation
- Added `StatsServiceTests.cs` (9 tests) — uses EF Core InMemory, covers all count aggregations + groupings
- Added `UpdateTaskRequestValidatorTests.cs` (10 tests) — mirrors `CreateTaskRequestValidator` coverage
- Added `AuditApiTests.cs` (6 integration tests) — covers GET `/api/v1/audit` and GET `/api/v1/audit/summary`
- **Test pyramid totals:** Unit: 65 | Integration: 43 | E2E: 26 = **134 total tests**
- Files affected: `tests/TaskPilot.Tests.Unit/Services/AuditServiceTests.cs`, `tests/TaskPilot.Tests.Unit/Services/StatsServiceTests.cs`, `tests/TaskPilot.Tests.Unit/Validators/UpdateTaskRequestValidatorTests.cs`, `tests/TaskPilot.Tests.Integration/Audit/AuditApiTests.cs`

---

## 2026-03-26 — Folder Restructure & Namespace Rename

### Refactor | Restructured project layout and renamed all namespaces
- Moved project from `src/TaskPilot.Server/` directly into `src/`; renamed `TaskPilot.Server.csproj` → `TaskPilot.csproj`
- Renamed `DTOs/` → `Models/` (preserving ApiKeys, Audit, Common, Stats, Tags, Tasks subfolders)
- Moved `Enums/` → `Models/Enums/`, `Validators/` → `Models/Validators/`
- Merged `Auth/` files into `Extensions/` (no separate Auth folder)
- Namespace changes throughout all `.cs` and `.cshtml` files:
  - `TaskPilot.Server.Auth` → `TaskPilot.Extensions`
  - `TaskPilot.Server` → `TaskPilot`
  - `TaskPilot.Shared.DTOs.*` → `TaskPilot.Models.*`
  - `TaskPilot.Shared.Enums` → `TaskPilot.Models.Enums`
  - `TaskPilot.Shared.Constants` → `TaskPilot.Constants`
  - `TaskPilot.Shared.Validators` → `TaskPilot.Models.Validators`
- Updated `TaskPilot.slnx`, `TaskPilot.Tests.Unit.csproj`, and `TaskPilot.Tests.Integration.csproj` project references
- Added `using TaskStatus = TaskPilot.Models.Enums.TaskStatus` alias in files where ambiguity with `System.Threading.Tasks.TaskStatus` arose
- Build verified: 0 errors, 0 warnings across all 4 projects
- **Files affected:** All `.cs` and `.cshtml` files under `src/` and `tests/`; `TaskPilot.slnx`; `tests/TaskPilot.Tests.Unit/TaskPilot.Tests.Unit.csproj`; `tests/TaskPilot.Tests.Integration/TaskPilot.Tests.Integration.csproj`

---

## 2026-03-26 — Documentation & Project Cleanup (Post-MVC Pivot)

### Docs | Updated DESIGN-SYSTEM.md for Bootstrap 5 + Bootstrap Icons
- Section 7 (Iconography): Replaced Phosphor Icons / Blazor implementation with Bootstrap Icons CDN; updated icon name mapping table
- Section 9 (UI Component Library): Replaced MudBlazor decision with Bootstrap 5 + HTMX + ApexCharts (all CDN); added comparison table and CDN snippet
- **Files:** `DESIGN-SYSTEM.md`

### Docs | Updated ARCHITECTURE.md for Razor Pages structure
- Section 1 (Solution Structure): Replaced `TaskPilot.Client/` subtree with `Pages/` Razor Pages subtree; removed bUnit test folder from unit test tree
- Section 4.1: "Blazor UI" → "Razor Pages web UI"
- Section 4.7 (CORS): Updated code sample to match actual `SetIsOriginAllowed` implementation
- Section 7 (Azure Migration): Updated Static Files and CORS rows; updated WebSockets note
- Section 9 (Package Decisions): Replaced ApexCharts.Blazor and MudBlazor sections with CDN-based stack; removed `TaskPilot.Client` and bUnit/MudBlazor from package lists
- **Files:** `ARCHITECTURE.md`

### Docs | Updated REQUIREMENTS.md iteration 2 plan
- Changed "Azure App Service (hosted Blazor WASM)" → "Azure App Service (ASP.NET Core Razor Pages + API)"
- **Files:** `REQUIREMENTS.md`

### Docs | Updated TEST-CASES.md for Razor Pages
- Section 4: Replaced bUnit component tests with a note that page model logic is covered by integration + E2E tests
- Section 12 (Playwright E2E): Replaced speculative test cases with the 23 actual implemented tests across Auth, Dashboard, Tasks, Settings, Audit test files
- **Files:** `TEST-CASES.md`

### Refactor | Deleted orphaned Blazor WASM project and tests
- Deleted `src/TaskPilot.Client/` (entire Blazor WASM project — App.razor, Pages, Components, Services, wwwroot)
- Deleted `tests/TaskPilot.Tests.Unit/Components/TaskSlideOverTests.cs` (bUnit test for removed Blazor component)
- Removed `bunit`, `MudBlazor`, and `TaskPilot.Client` references from `TaskPilot.Tests.Unit.csproj`
- **Files:** `tests/TaskPilot.Tests.Unit/TaskPilot.Tests.Unit.csproj`

---

## 2026-03-26 — Architectural Pivot: Blazor WebAssembly → Razor Pages MVC

### Architecture | Replaced Blazor WebAssembly frontend with ASP.NET Core Razor Pages
- Removed `TaskPilot.Client` (Blazor WASM) project from solution; `TaskPilot.Server` now serves both API and UI
- Blazor WASM caused E2E test failures: fingerprinted `_framework/` URLs (404), CORS blocking ES module loads, `document.currentScript` null in module context, MudBlazor shadow DOM selectors, 40+ minute test runs with 1/24 passing
- Razor Pages delivers server-rendered HTML — Playwright tests run in ~28 seconds, all 23 pass
- **Files affected:** `TaskPilot.slnx`, `src/TaskPilot.Server/TaskPilot.Server.csproj`

### Feature | Built complete Razor Pages frontend (13 page pairs)
- **Auth:** `Pages/Auth/Login.cshtml`, `Register.cshtml`, `Logout.cshtml` — cookie auth, `[AllowAnonymous]`, `_LoginLayout`
- **Dashboard:** `Pages/Index.cshtml` — 5 summary cards, ApexCharts (weekly bar + priority donut), quick-add form, recent tasks table
- **Tasks:** `Pages/Tasks/Index.cshtml` — list/board toggle, HTMX search, status/priority filters, New Task modal; `Pages/Tasks/Detail.cshtml` — full edit, complete, delete
- **Settings:** `Pages/Settings/Index.cshtml` — API key management (generate/activate/deactivate/revoke), change password, appearance section
- **Audit:** `Pages/Audit/Index.cshtml` — 4 summary cards, filterable audit log table
- **Shared:** `Pages/Shared/_Layout.cshtml` (sidebar nav, toast notifications), `Pages/Shared/_LoginLayout.cshtml`
- **Files:** All `src/TaskPilot.Server/Pages/**/*.cshtml` and `*.cshtml.cs`

### Design | Custom CSS design system with Bootstrap 5 + HTMX + ApexCharts (CDN)
- CSS variables: `--tp-purple: #6255EC`, sidebar, stats grid, kanban, badges, auth, settings, audit
- No npm/build pipeline — all dependencies via CDN; Bootstrap Icons for iconography
- **Files:** `src/TaskPilot.Server/wwwroot/css/app.css`, `Pages/Shared/_Layout.cshtml`

### Architecture | Updated Program.cs and ServiceCollectionExtensions for Razor Pages
- Added `AddRazorPages()` with authorize-all convention, `AllowAnonymousToPage` for auth/error
- `ConfigureApplicationCookie()`: `/auth/login` redirect for web, 401 for `/api` paths
- CORS updated: `SetIsOriginAllowed(origin => uri.Host == "localhost")` for local dev
- Same-process service injection in PageModels (no HTTP overhead, API controllers remain for LLM clients)
- **Files:** `src/TaskPilot.Server/Program.cs`, `src/TaskPilot.Server/Extensions/ServiceCollectionExtensions.cs`

### Test | Updated all 23 E2E Playwright tests for Razor Pages selectors
- Updated `PlaywrightFixture.cs`: navigates to `/auth/register`, waits for `input[type='email']`
- All test files updated: Auth (5), Dashboard (5), Tasks (7), Settings (4), Audit (2)
- Fixed `Guid.NewGuid().ToString("N")[..8]` slice syntax in all test files
- **Files:** `tests/TaskPilot.Tests.E2E/**/*.cs`

### Fix | Resolved multiple C# compilation issues in Razor Pages
- `using TaskStatus = TaskPilot.Shared.Enums.TaskStatus;` alias to resolve ambiguity with `System.Threading.Tasks.TaskStatus`
- Renamed `AuditIndexModel.Page` → `CurrentPage` (conflicts with `PageModel.Page()` method)
- All DTOs use positional record constructor syntax (no object initializers)
- Removed `ResultAnalysis` from `UpdateTaskRequest` (belongs to `CompleteTaskRequest` only)

---

## 2026-03-26 — Phase 5: Polish + README + Git Init

### Docs | Created README.md
- Setup instructions, quick start, API reference, project structure, tech stack table, iteration 2 roadmap, security notes
- **Files:** `README.md`

### Config | Created .gitignore
- Covers .NET build artifacts, SQLite DB files, VS/Rider IDE files, Playwright output, Serilog log files
- **Files:** `.gitignore`

### Config | Initialized git repository
- `git init` + initial commit with all project files
- Commit: `b59b9c7 Initial commit — TaskPilot v1 (Iteration 1)`

---

## 2026-03-26 — Phase 4: Testing & Security Validation

### Test | Wrote all unit tests (36 tests, all passing)
- **Services:** `TaskServiceTests` (12 tests), `TagServiceTests` (5 tests), `ApiKeyServiceTests` (6 tests) — Moq-based isolation tests
- **Validators:** `CreateTaskRequestValidatorTests` (6 tests), `CreateTagRequestValidatorTests` (4 tests) — direct validator tests
- **Components:** `TaskSlideOverTests` (3 bUnit tests) — MudBlazor component rendering tests with async disposal
- Added `TaskPilot.Client` project reference to `TaskPilot.Tests.Unit` for bUnit tests
- Files: `tests/TaskPilot.Tests.Unit/Services/**`, `tests/TaskPilot.Tests.Unit/Validators/**`, `tests/TaskPilot.Tests.Unit/Components/**`

### Test | Wrote all integration tests (36 tests, all passing)
- **WebAppFactory:** SQLite per-test isolation with `IAsyncLifetime`, HMAC secret override
- **Auth:** `AuthApiTests` (10 tests) — register, login, logout, me, API key auth scenarios
- **Tasks:** `TasksApiTests` (15 tests) — full CRUD, search, filter, complete, soft-delete, stats
- **Tags:** `TagsApiTests` (5 tests) — create, duplicate, list, delete, cross-user isolation
- **API Keys:** `ApiKeysApiTests` (6 tests) — generate, list, deactivate, activate, revoke
- Helper: `AuthHelper.CreateAuthenticatedClientAsync` for test user provisioning
- Files: `tests/TaskPilot.Tests.Integration/**`

### Fix | Resolved production bugs discovered during testing
- **TaskService:** Removed redundant `taskRepository.Update(task)` calls for tracked entities — caused `DbUpdateConcurrencyException` on EF change tracking (UpdateTask, PatchTask, CompleteTask, DeleteTask, UpdateSortOrder)
- **TaskService:** Same fix for `tagRepository.Update` and `apiKeyRepository.Update` calls in TagService/ApiKeyService
- **TaskActivityLog:** Removed `Guid.NewGuid()` from `Id` property initializer — EF was inferring pre-set Guid as Modified instead of Added, causing UPDATE instead of INSERT
- **TaskActivityLogConfiguration:** Added `ValueGeneratedOnAdd()` for `Id` property
- **StatsService:** Rewrote complex LINQ GroupBy queries to load data client-side then aggregate — SQLite provider cannot translate `DayOfYear`, `TotalDays` arithmetic, and complex group-by projections
- **ServiceCollectionExtensions:** Fixed multi-scheme authentication — added `PolicyScheme` that forwards to `ApiKey` scheme when `X-Api-Key` header is present, otherwise to `Cookie` scheme. Previous setup only tried the default cookie scheme, breaking API key auth
- **Program.cs:** Removed `return 0/1` pattern — replaced with void-returning try/catch to make `WebApplicationFactory` host interception work correctly; added `public partial class Program { }`
- Files: `src/TaskPilot.Server/Services/TaskService.cs`, `src/TaskPilot.Server/Services/TagService.cs`, `src/TaskPilot.Server/Services/ApiKeyService.cs`, `src/TaskPilot.Server/Services/StatsService.cs`, `src/TaskPilot.Server/Entities/TaskActivityLog.cs`, `src/TaskPilot.Server/Data/Configurations/TaskActivityLogConfiguration.cs`, `src/TaskPilot.Server/Extensions/ServiceCollectionExtensions.cs`, `src/TaskPilot.Server/Program.cs`

### Security | Created Security Validation document
- All 37 checks pass; 2 N/A items deferred to iteration 2 (Key Vault, rate limiting)
- Files: `SECURITY-VALIDATION.md`

---

## 2026-03-26 — Phase 3: Blazor Frontend

### Feature | Built complete Blazor WebAssembly frontend
- **Services (7):** `IHttpClientService`/`HttpClientService` (unwraps `ApiResponse<T>`), `TaskHttpService`, `TagHttpService`, `ApiKeyHttpService`, `AuditHttpService`, `AuthService` (cookie auth via `/account/me`), `ToastService` (auto-dismiss, undo, max 3), `ThemeService` (localStorage, MudTheme with `#6255EC` palette)
- **Layout:** `MainLayout` with MudBlazor drawer, persistent sidebar (desktop), QuickAddBar in AppBar, auth redirect, theme toggle
- **Pages (7):** `Login`, `Register`, `Dashboard` (5 summary cards + 7 ApexCharts with skeleton), `Tasks` (list/board toggle, search debounce 300ms, filters, bulk actions, inline edit), `TaskDetail` (activity log), `Audit` (summary cards + filtered table + pagination), `Settings` (4 tabs: API Keys, Appearance, Export, Account)
- **Components:** `TaskSlideOver` (full create/edit with all fields, tag multi-select, Save & Create Another), `ToastContainer`, `QuickAddBar`, `EmptyState`, `SkeletonLoader`
- **wwwroot:** `index.html` with MudBlazor CSS/JS, Plus Jakarta Sans + DM Sans fonts, Phosphor Icons CDN; `app.css` with all CSS variables, slide-over animation, toast slide-up, responsive breakpoints
- **Build result:** `dotnet build` → 0 warnings, 0 errors (all 6 projects)
- **Files:** `src/TaskPilot.Client/Program.cs`, `src/TaskPilot.Client/_Imports.razor`, `src/TaskPilot.Client/App.razor`, `src/TaskPilot.Client/Services/**`, `src/TaskPilot.Client/Pages/**`, `src/TaskPilot.Client/Components/**`, `src/TaskPilot.Client/wwwroot/**`

---

## 2026-03-26 — Phase 2: Backend Scaffolding

### Architecture | Created all Shared project types
- Enums: `TaskPriority`, `TaskStatus`, `TargetDateType`, `RecurrencePattern`
- DTOs: `TaskResponse`, `CreateTaskRequest`, `UpdateTaskRequest`, `PatchTaskRequest`, `CompleteTaskRequest`, `TaskQueryParams`, `TagResponse`, `CreateTagRequest`, `ApiKeyResponse`, `CreateApiKeyResponse`, `CreateApiKeyRequest`, `RenameApiKeyRequest`, `AuditLogResponse`, `AuditQueryParams`, `AuditSummaryResponse`, `TaskStatsResponse` (with 7 sub-records)
- Response envelope: `ApiResponse<T>`, `PagedApiResponse<T>`, `ResponseMeta`, `PagedResponseMeta`, `ErrorResponse`, `ApiError`, `FieldError`
- Constants: `TaskTypes`, `ApiRoutes`
- FluentValidation validators: `CreateTaskRequestValidator`, `UpdateTaskRequestValidator`, `CreateTagRequestValidator`, `CreateApiKeyRequestValidator`
- **Files:** `src/TaskPilot.Shared/Enums/*.cs`, `src/TaskPilot.Shared/DTOs/**/*.cs`, `src/TaskPilot.Shared/Constants/*.cs`, `src/TaskPilot.Shared/Validators/*.cs`

### Architecture | Created all Server entities
- `BaseEntity` with auto-set `CreatedDate`, `LastModifiedDate`, `LastModifiedBy` via `SaveChangesAsync` override
- `TaskItem`, `Tag`, `TaskTag`, `ApiKey`, `ApiAuditLog`, `TaskActivityLog`
- EF entity configurations for all entities: proper indexes (11 total), soft-delete query filter on `TaskItem`, composite unique index on `Tag.Name+UserId`, HMAC hash unique index on `ApiKey.KeyHash`
- `ApplicationDbContext` extends `IdentityDbContext`, applies all configurations via reflection
- **Files:** `src/TaskPilot.Server/Entities/*.cs`, `src/TaskPilot.Server/Data/Configurations/*.cs`, `src/TaskPilot.Server/Data/ApplicationDbContext.cs`

### Architecture | Created repository and service layer
- Generic `IRepository<T>` + `GenericRepository<T>` base
- Specialized: `ITaskRepository`/`TaskRepository` (paged query with all filters), `ITagRepository`/`TagRepository`, `IApiKeyRepository`/`ApiKeyRepository` (with `ExecuteUpdateAsync` for non-blocking LastUsedDate), `IAuditLogRepository`/`AuditLogRepository`
- Services: `ITaskService`/`TaskService` (full CRUD + complete + recurring successor), `ITagService`/`TagService`, `IApiKeyService`/`ApiKeyService` (HMAC-SHA256 key hashing), `IStatsService`/`StatsService` (7 chart aggregations), `IAuditService`/`AuditService`
- **Files:** `src/TaskPilot.Server/Repositories/**/*.cs`, `src/TaskPilot.Server/Services/**/*.cs`

### Architecture | Created all controllers
- `BaseApiController` with `UserId`, `ModifiedBy`, `Envelope<T>`, `NotFoundError`, `ValidationError`, `ConflictError` helpers
- `TasksController`, `TagsController`, `ApiKeysController`, `AuditController`, `AccountController`
- All controllers use `[Authorize]`; cookie auth for web UI, API key auth for LLM clients
- **Files:** `src/TaskPilot.Server/Controllers/*.cs`

### Architecture | Created auth, middleware, extensions
- `ApiKeyAuthenticationHandler` — validates `X-Api-Key` header via HMAC hash lookup
- `ApiAuditMiddleware` — SHA256 hashes request body, writes non-blocking audit log for all API key requests
- `GlobalExceptionMiddleware` — catches unhandled exceptions, returns standard error envelope
- `ServiceCollectionExtensions`, `ApplicationBuilderExtensions` — clean DI registration
- **Files:** `src/TaskPilot.Server/Auth/*.cs`, `src/TaskPilot.Server/Middleware/*.cs`, `src/TaskPilot.Server/Extensions/*.cs`

### Config | Rewrote Program.cs and appsettings
- Full DI pipeline: Serilog, EF/Identity, cookie + API key auth, CORS, Swagger (dev), all services/repos/validators
- `appsettings.json` with SQLite connection string and Serilog config
- HMAC secret stored via `dotnet user-secrets` (never hardcoded)
- `dotnet build` → **Build succeeded. 0 Warnings. 0 Errors.** (all 6 projects)
- **Files:** `src/TaskPilot.Server/Program.cs`, `src/TaskPilot.Server/appsettings*.json`

---

## Unreleased (Phase 3+)

_Changes will be logged here as they occur._

---

## 2026-03-26 — Doc Structure Improvements (post Phase 1)

### Docs | Restructured CLAUDE.md as project command center
- Added Document Index table linking to all spec files
- Added mandatory Doc-Update Protocol (agents must update docs + CHANGELOG on every change)
- Added Build Phases tracker
- Added Agent Authority Matrix with doc ownership
- Retained all coding conventions and architecture patterns
- **Files:** `CLAUDE.md`

### Docs | Created REQUIREMENTS.md
- Extracted all feature requirements, data model, and API contract from original project prompt
- Separates WHAT to build (REQUIREMENTS.md) from HOW to build it (ARCHITECTURE.md)
- **Files:** `REQUIREMENTS.md`

### Test | Created TEST-CASES.md
- Dedicated test case specification: 100+ test cases across unit (services, validators, repos), bUnit components, integration (all HTTP endpoints + auth + middleware), Playwright E2E (auth, task lifecycle, dashboard, settings, keyboard shortcuts, responsive), and security validation
- **Files:** `TEST-CASES.md`

### Docs | Created CHANGELOG.md
- Established this audit log for all project changes
- **Files:** `CHANGELOG.md`

---

## 2026-03-26 — Phase 1: Architecture & Design

### Docs | Created project documentation hub
- Rewrote `CLAUDE.md` from style guide into command center with full document index and mandatory doc-update protocol
- Added: Document index table, Doc-Update Protocol section, Build Phases tracker, links to all spec files
- **Files:** `CLAUDE.md`

### Docs | Created REQUIREMENTS.md
- Extracted all feature requirements, data model, and API contract from original project prompt into standalone requirements document
- Separates WHAT to build (REQUIREMENTS.md) from HOW to build it (ARCHITECTURE.md)
- **Files:** `REQUIREMENTS.md`

### Docs | Created CHANGELOG.md
- Created this file for auditing all changes going forward
- **Files:** `CHANGELOG.md`

### Test | Created TEST-CASES.md
- Created dedicated test case specification file covering all unit, integration, bUnit, and Playwright E2E test cases
- Extracted and expanded from `qa-engineer.md` agent spec
- **Files:** `TEST-CASES.md`

### Architecture | Created ARCHITECTURE.md
- Full system architecture specification: solution structure (6-project directory tree), Mermaid ER diagram, API endpoint specs with DTO shapes, security design (cookie auth, API key auth handler, HMAC-SHA256, audit middleware, soft-delete, CORS), coding standards with C# code snippets, DB indexing strategy (11 indexes), Azure migration map, NuGet package list for all projects, configuration guide
- **Key decisions:** ApexCharts.Blazor for charts (best feature set), MudBlazor for UI components (component completeness + accessibility)
- **Files:** `ARCHITECTURE.md`

### Design | Created DESIGN-SYSTEM.md
- Complete visual language specification: color palette (light + dark mode, exact hex values), Plus Jakarta Sans + DM Sans typography, spacing/radius/shadow/motion tokens, Phosphor Icons, all component tokens (buttons, inputs, cards, badges, toasts, slide-over), MudBlazor component library decision, WCAG AA accessibility requirements
- **Key decisions:** Primary color `#6255EC` (deep violet-indigo, Linear-inspired), Phosphor Icons (multiple weight variants), Plus Jakarta Sans + DM Sans fonts
- **Files:** `DESIGN-SYSTEM.md`

### Design | Created WIREFRAMES.md
- All 12 pages wireframed at 3 breakpoints (Desktop ≥1025px, Tablet 641–1024px, Mobile ≤640px): Login/Register, Dashboard, Task List (List + Board views), Task Create/Edit slide-over, Task Detail, LLM Audit Dashboard, Settings, Empty States, Onboarding, Keyboard Shortcuts overlay, 404/Error pages
- **Files:** `WIREFRAMES.md`

### Design | Created USER-FLOWS.md
- 12 complete user flows: Registration/onboarding, Quick-add, Search/filter, Complete with result analysis, Generate API key, Audit review, Bulk actions, Theme switch, CSV export, Drag-drop, Undo delete, Full task lifecycle
- Interaction pattern specs: slide-over animations, toast behavior, drag-drop mechanics, swipe gestures, skeleton loaders, error states
- **Files:** `USER-FLOWS.md`

### Config | Created agent definitions
- Created 4 subagent files in `.claude/agents/`: `architect.md`, `ux-designer.md`, `fullstack-dev.md`, `qa-engineer.md`
- Each agent has defined tools, model, and full system prompt
- **Files:** `.claude/agents/architect.md`, `.claude/agents/ux-designer.md`, `.claude/agents/fullstack-dev.md`, `.claude/agents/qa-engineer.md`

---

## Template for future entries

```
### [Type] | [Short description]
- [What changed and why]
- [Any decisions made or trade-offs]
- **Files:** `path/to/file.ext`, `path/to/other.ext`
```
