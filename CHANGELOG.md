# TaskPilot Changelog

> All changes, decisions, and deviations are logged here in reverse chronological order.
> Format: `[YYYY-MM-DD] TYPE: Description — Files affected`
>
> **Types:** Feature | Fix | Design | Architecture | Refactor | Security | Test | Docs | Config

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
