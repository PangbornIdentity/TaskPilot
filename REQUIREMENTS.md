# TaskPilot — Requirements

> This document captures the full feature requirements, data model, and behavioral specs for TaskPilot.
> It is the source of truth for WHAT to build. ARCHITECTURE.md covers HOW to build it.

---

## Table of Contents
1. [Product Overview](#1-product-overview)
2. [Iteration Strategy](#2-iteration-strategy)
3. [Data Model](#3-data-model)
4. [Web UI Features](#4-web-ui-features)
5. [REST API Features](#5-rest-api-features)
6. [Constraints](#6-constraints)

---

## 1. Product Overview

TaskPilot is a personal productivity web application for managing tasks and todos across work and personal life.

**Two access channels:**

| Channel | Users | Purpose |
|---------|-------|---------|
| Web UI | Owner (me) | Create, edit, view, complete, and analyze tasks. Responsive — works on desktop, tablet, mobile. |
| REST API | External LLMs (ChatGPT, Claude, Copilot, etc.) | Read and write tasks on the owner's behalf via API keys. All LLM activity is audited on a dedicated screen. |

---

## 2. Iteration Strategy

### Iteration 1 (this build)
- **Hosting**: Local development on localhost
- **Database**: SQLite
- **Secrets**: `dotnet user-secrets`
- **Run**: `dotnet run`
- **Scope**: Full functionality, ready for testing and verification
- **NOT included**: Rate limiting (will be added in iteration 2)

### Iteration 2 (future)
- Azure App Service (ASP.NET Core Razor Pages + API)
- Azure SQL Database or PostgreSQL Flexible Server
- Azure Key Vault for secrets
- Application Insights for monitoring
- GitHub Actions CI/CD
- Per-API-key rate limiting
- PWA support

**Critical constraint**: Every architectural decision in iteration 1 must be Azure-migration-ready. No shortcuts that create migration debt. Local → Azure must be a configuration change, not a rewrite.

---

## 3. Data Model

### 3.1 Task

| Field | Type | Rules |
|-------|------|-------|
| Id | GUID | Primary key, server-generated |
| Title | string | Required, max 200 chars |
| Description | string? | Optional, supports markdown rendering in UI |
| TaskTypeId | int? | FK to `TaskType` lookup table (§3.7); optional |
| Area | enum | `Personal=0`, `Work=1` — default `Personal`; required |
| Priority | enum | `Critical`, `High`, `Medium`, `Low` |
| Status | enum | `NotStarted`, `InProgress`, `Blocked`, `Completed`, `Cancelled` |
| TargetDateType | enum | `SpecificDay`, `ThisWeek`, `ThisMonth` |
| TargetDate | DateTime? | Actual target date |
| CompletedDate | DateTime? | Auto-set when Status transitions to `Completed` |
| ResultAnalysis | string? | Post-completion reflection — what went well, what didn't, what I'd do differently |
| IsRecurring | bool | Whether this task auto-creates a successor on completion |
| RecurrencePattern | enum? | `Daily`, `Weekly`, `Monthly` — null if not recurring |
| SortOrder | int | Manual drag-and-drop ordering within views |
| IsDeleted | bool | Soft delete flag — NEVER hard-delete from UI or API |
| DeletedAt | DateTime? | Timestamp of soft-delete, for 30-second undo window |
| CreatedDate | DateTime | Auto-set on creation, never modified |
| LastModifiedDate | DateTime | Auto-updated on every field change |
| LastModifiedBy | string | `"user:{username}"` or `"api:{apiKeyName}"` |
| UserId | string | FK to Identity user (multi-user ready, iteration 1 is single-user) |

### 3.2 Tag

| Field | Type | Rules |
|-------|------|-------|
| Id | GUID | PK |
| Name | string | Unique per user |
| Color | string | Hex value (e.g., `#6255EC`) |
| UserId | string | FK |
| CreatedDate | DateTime | Auto-set |
| LastModifiedDate | DateTime | Auto-updated |
| LastModifiedBy | string | Same format as Task |

> **Note:** `Tag` and `TaskTag` are now fully UI-exposed. Tags are shown as coloured pills on task cards, task detail, and the create/edit form. Previously the data layer existed but tags were not surfaced in the UI.

### 3.3 TaskTag (join table)

| Field | Type |
|-------|------|
| TaskId | GUID (FK) |
| TagId | GUID (FK) |

### 3.4 ApiKey

| Field | Type | Rules |
|-------|------|-------|
| Id | GUID | PK |
| Name | string | Display label, e.g., "ChatGPT-Work" |
| KeyHash | string | HMAC-SHA256 hash — NEVER store plaintext |
| KeyPrefix | string | First 8 chars of the key, for display identification |
| CreatedDate | DateTime | Auto-set |
| LastUsedDate | DateTime? | Updated non-blocking on each use |
| IsActive | bool | Inactive keys are rejected immediately |
| UserId | string | FK |
| LastModifiedDate | DateTime | Auto-updated |
| LastModifiedBy | string | Same format as Task |

### 3.5 ApiAuditLog

| Field | Type | Rules |
|-------|------|-------|
| Id | GUID | PK |
| ApiKeyId | GUID (FK) | FK to ApiKey |
| ApiKeyName | string | Denormalized for query performance (key may be deleted) |
| Timestamp | DateTime | UTC |
| HttpMethod | string | GET, POST, PUT, PATCH, DELETE |
| Endpoint | string | Path without query string |
| RequestBodyHash | string | SHA256 hash of request body — NEVER store full body |
| ResponseStatusCode | int | HTTP status code |
| DurationMs | long | Request duration in milliseconds |
| UserId | string | FK |

### 3.6 TaskActivityLog

| Field | Type | Rules |
|-------|------|-------|
| Id | GUID | PK |
| TaskId | GUID (FK) | FK to TaskItem |
| Timestamp | DateTime | UTC |
| FieldChanged | string | Name of the field that changed |
| OldValue | string? | Previous value as string |
| NewValue | string? | New value as string |
| ChangedBy | string | Same format as LastModifiedBy |

### 3.7 TaskType (lookup table)

| Field | Type | Rules |
|-------|------|-------|
| Id | int | PK, identity |
| Name | string | Display name, unique, required |
| SortOrder | int | Controls sort order in dropdowns |
| IsActive | bool | Inactive types are hidden from dropdowns but retained for existing tasks |

**Seed data (applied at startup):**

| Id | Name | SortOrder | IsActive |
|----|------|-----------|----------|
| 1 | Task | 1 | true |
| 2 | Goal | 2 | true |
| 3 | Habit | 3 | true |
| 4 | Meeting | 4 | true |
| 5 | Note | 5 | true |
| 6 | Event | 6 | true |

TaskType records are read-only in iteration 1 (no UI to add/edit types). Exposed via `GET /api/v1/task-types`.

---

## 4. Web UI Features

### 4.1 Dashboard (Home Page)

**Summary cards (top row):**
- Total Active Tasks
- Completed Today
- Overdue (past target date + not completed)
- In Progress
- Blocked

**Charts section:**
| Chart | Type | Data |
|-------|------|------|
| Tasks completed per week | Bar | Last 12 weeks |
| Tasks completed per month | Bar | Last 12 months |
| Tasks completed per year | Bar | All available years |
| Completion rate over time | Line | % completed vs created, by week |
| Breakdown by Type | Donut | All active tasks |
| Breakdown by Priority | Stacked bar | All active tasks |
| Average time-to-completion trend | Line | Time from creation to completion, by week |
| Completions by Area | Bar | Personal vs Work completion count |
| Top 5 tags by task count | Bar | All active tasks |
| Task count by type | Donut | All active tasks grouped by TaskType |

**Quick-add bar:** Persistent at top of every page.

### 4.2 Task List View

- **Default grouping:** By status, sorted by priority within each group
- **View toggle:** List view (dense table) / Board/Kanban view (columns by status)
- **Search:** Full-text across title + description (debounced 300ms)
- **Filters (combinable):** Area (Personal / Work / All), TaskType (dropdown, single-select, sourced from `/api/v1/task-types`), Tags (multi-select chip picker, AND logic — task must have ALL selected tags), priority, status, date range (target date), recurring only
- **Sort options:** Priority, target date, created date, last modified date (ascending/descending)
- **Bulk actions toolbar** (when items selected): mark complete, change priority, change status, add/remove tag, soft-delete
- **Drag-and-drop reordering** within groups (persists `SortOrder`)
- **Inline edit:** Click title to rename in place
- **Mobile swipe:** Right = mark complete, Left = soft-delete with undo toast

### 4.3 Task Create / Edit

- Opens as a **slide-over panel** from the right (not a separate page)
- All task fields with appropriate inputs (text, dropdowns, date pickers, tag multi-select)
- **Area** is a required toggle/select (Personal / Work), defaulting to Personal
- **TaskType** is an optional dropdown sourced from `GET /api/v1/task-types` (active types only, sorted by `SortOrder`)
- Tag selector with inline "create new tag" capability (type name, pick color)
- Tags displayed as coloured pills in the selector and on the saved form
- **"Save & Create Another"** button for rapid batch entry
- **Auto-save draft** to localStorage if user navigates away mid-edit
- Inline validation feedback on each field

### 4.4 Task Detail View

- Full read view of all task fields
- **Tags** displayed as coloured pills beneath the title/description
- **Result Analysis** prominently displayed when status = Completed (prompt to fill in if empty)
- **Activity log:** Chronological list of all field changes (from `TaskActivityLog`)
- Edit button → opens edit slide-over

### 4.5 LLM Audit Dashboard

- Data table: timestamp, API key name, HTTP method, endpoint, status code, duration (ms)
- **Filters:** API key (dropdown), date range, HTTP method, status code range
- **Summary cards:** Total requests, GETs today, writes (POST/PUT/PATCH/DELETE) today, active API keys
- **Per-API-key chart:** Bar chart, request count per key, last 30 days
- Click an API key name → filters table to that key's activity
- Pagination on audit log table

### 4.6 Settings Page

**API Key Management:**
- Generate new API key: enter name/label → receive key displayed ONCE (copy button + "won't be shown again" warning)
- Keys table: name, prefix (8 chars), created date, last used date, status
- Per-key actions: rename, deactivate/activate, revoke (permanent)

**Appearance:**
- Light / Dark / System mode selector
- Persists to `localStorage`

**Data Export:**
- Export tasks as CSV

**Account:**
- Change password

### 4.7 Integrations Page (`/integrations`)

**Purpose:** Teach users how to connect LLMs and automation tools to TaskPilot using the REST API.

**Sections:**
- **Quick Start:** 3-step guide (create API key → note base URL → add header), curl example with copy button
- **REST API Reference:** Endpoint table (method, path, description); Swagger link in dev mode only
- **Claude (Anthropic):** Copy-ready `list_tasks` and `create_task` tool definitions in Anthropic tool-use JSON format
- **OpenAI / GPT:** Copy-ready `list_tasks` and `create_task` function definitions in OpenAI function-calling JSON format
- **MCP (Coming Soon):** Informational placeholder; no MCP endpoint is implemented in iteration 1

**Access:** Authenticated users only. Unauthenticated users are redirected to login.

**Swagger link visibility:** Link to `/swagger` is shown only in Development environment. In Production a text note is shown instead.

**Future work (Iteration 2):** MCP server at `/mcp` using `ModelContextProtocol` NuGet package, protected by existing API key authentication.

### 4.8 Interaction & UX Behaviors

**Keyboard shortcuts:**
| Key | Action |
|-----|--------|
| `N` | New task (open create panel) |
| `E` | Edit selected task |
| `/` | Focus search |
| `Esc` | Close slide-over/modal |
| `?` | Show keyboard shortcuts overlay |
| `Space` | Toggle selected task complete |

**Quick-add bar:**
- Persistent on every page
- Type title + Enter = create task with defaults
- Type title + Tab = open full create form with title pre-filled

**Toast notifications:**
- Bottom-right, stack upward, max 3 visible
- Auto-dismiss: success/info 5s, error 8s, undo 30s
- Undo toast has a visible countdown progress bar

**Soft-delete with undo:**
- Deleting shows an undo toast for 30 seconds
- After 30 seconds, soft-delete is permanent
- Undo toast persists across navigation within the 30s window

**Empty states:** Illustration + helpful copy + CTA for: no tasks, no filter results, no audit logs, no API keys.

**Skeleton loading:** Every data-fetching view shows a skeleton placeholder matching the real layout.

**Onboarding (first login):**
- Create 3 sample tasks (Work/Personal/Completed with ResultAnalysis)
- Dismissible welcome banner with 3 feature callouts

**Responsive breakpoints:**
| Breakpoint | Behavior |
|------------|----------|
| Mobile ≤640px | Bottom tab navigation, full-width cards, swipe gestures |
| Tablet 641–1024px | Collapsible sidebar (icon rail), 2-column layouts |
| Desktop ≥1025px | Persistent sidebar, dense information layout |

**Animations:** Checkbox spring bounce on completion. Slide-over animate from right. Skeleton pulses. Toasts slide up. Tasteful — no gratuitous animation.

---

## 5. REST API Features

**Base path:** `/api/v1/`
**Authentication:** `X-Api-Key` header

### 5.1 Task Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/v1/tasks | List tasks (filterable, pageable, sortable) |
| GET | /api/v1/tasks/{id} | Get single task |
| POST | /api/v1/tasks | Create task |
| PUT | /api/v1/tasks/{id} | Full update (all fields) |
| PATCH | /api/v1/tasks/{id} | Partial update (changed fields only) |
| DELETE | /api/v1/tasks/{id} | Soft-delete |
| POST | /api/v1/tasks/{id}/complete | Mark complete. Optional: `{ "resultAnalysis": "..." }` |
| GET | /api/v1/tasks/stats | Aggregated stats |

**GET /api/v1/tasks query params:** `status`, `taskTypeId` (int), `area` (enum: `Personal`, `Work`), `priority`, `search`, `tags` (comma-sep tag names, AND logic), `isRecurring`, `page`, `pageSize`, `sortBy`, `sortDir`

### 5.2 TaskType Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/v1/task-types | List all active TaskType records (sorted by SortOrder) |

Read-only in iteration 1. No create/update/delete endpoints.

### 5.3 Tag Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/v1/tags | List all tags |
| POST | /api/v1/tags | Create tag |
| DELETE | /api/v1/tags/{id} | Delete tag (removes from all tasks) |

### 5.4 Response Envelope

All API responses use the standard envelope. No exceptions.

**Success (single):** `{ "data": {...}, "meta": { "timestamp": "...", "requestId": "guid" } }`

**Success (list):** `{ "data": [...], "meta": { "timestamp": "...", "requestId": "...", "page": 1, "pageSize": 20, "totalCount": 142, "totalPages": 8 } }`

**Error:** `{ "error": { "code": "VALIDATION_ERROR", "message": "...", "details": [{ "field": "title", "message": "..." }] } }`

### 5.5 API Behaviors

- All write operations set `LastModifiedBy` to `"api:{apiKeyName}"`
- All write operations create a `TaskActivityLog` entry
- **All requests** (read and write) are logged to `ApiAuditLog`
- API keys stored as HMAC-SHA256 hash — never plaintext. Full key shown ONCE on generation.
- Swagger UI at `/swagger` in Development, disabled in Production
- HTTP status codes: 200, 201, 204, 400, 401, 404, 409, 500
- **No rate limiting in iteration 1.** Middleware insertion point documented in ARCHITECTURE.md §4.8.

---

## 6. Constraints

1. Every entity MUST have: `CreatedDate`, `LastModifiedDate`, `LastModifiedBy` (via BaseEntity).
2. `LastModifiedBy` format: `"user:{username}"` for web UI, `"api:{apiKeyName}"` for API. Non-negotiable.
3. All REST endpoints under `/api/v1/`.
4. No business logic in controllers.
5. All secrets via `IConfiguration`: user-secrets locally, Key Vault in production. NEVER hardcode.
6. **Soft delete only.** Never hard-delete from UI or API. `IsDeleted` + `DeletedAt`.
7. No SQLite-specific features. Must work identically on SQLite, Azure SQL, PostgreSQL.
8. Standard response envelope on every API response. No exceptions.
9. DTOs and enums in `TaskPilot.Shared` only.
10. Git initialized. Commit at end of each phase.
11. Target `net10.0` in all `.csproj` files.
12. No rate limiting in iteration 1. Document insertion point only.
13. Windows development environment.
