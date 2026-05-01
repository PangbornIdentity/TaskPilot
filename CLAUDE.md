# TaskPilot — Command Center

> This is the master control document for the TaskPilot project.
> Every agent and every human contributor reads this file first.
> All project documentation is indexed here.

---

## Document Index

| Document | Purpose | Owner |
|----------|---------|-------|
| [REQUIREMENTS.md](./REQUIREMENTS.md) | Full feature requirements, data model, API contract, UX behaviors | Source of truth for WHAT to build |
| [ARCHITECTURE.md](./ARCHITECTURE.md) | Solution structure, ER diagram, API specs, security design, DB indexes, Azure migration map, packages | `architect` agent |
| [DESIGN-SYSTEM.md](./DESIGN-SYSTEM.md) | Colors, typography, spacing, components, iconography, accessibility | `ux-designer` agent |
| [WIREFRAMES.md](./WIREFRAMES.md) | Page layouts at all 3 breakpoints (Desktop/Tablet/Mobile) | `ux-designer` agent |
| [USER-FLOWS.md](./USER-FLOWS.md) | Step-by-step user journeys and interaction pattern specs | `ux-designer` agent |
| [TEST-CASES.md](./TEST-CASES.md) | All test cases: unit, integration, Playwright E2E | `qa-engineer` agent |
| [CHANGELOG.md](./CHANGELOG.md) | Chronological log of all changes, decisions, and deviations | All agents, every change |
| [SECURITY-VALIDATION.md](./SECURITY-VALIDATION.md) | Security checklist results (produced in Phase 5) | `qa-engineer` agent |
| [README.md](./README.md) | Setup, run, test, and deploy instructions (produced in Phase 6) | `architect` agent |

---

## Doc-Update Protocol (MANDATORY)

**Every time a change is made — whether to code, design, or requirements — the following must happen in the same operation:**

1. **CHANGELOG.md** — Add an entry with: date (YYYY-MM-DD), type (Feature / Fix / Design / Architecture / Refactor / Security / Test), brief description, and which files were affected.

2. **Affected documentation files** — Update any doc that is now inaccurate. Specifically:
   - New or changed API endpoint → update `ARCHITECTURE.md` (Section 3) AND `REQUIREMENTS.md`
   - New or changed entity/field → update `ARCHITECTURE.md` (Section 2 ER diagram + Section 3 DTOs)
   - New or changed UI behavior → update `WIREFRAMES.md` and/or `USER-FLOWS.md`
   - New or changed visual spec → update `DESIGN-SYSTEM.md`
   - New or changed test → update `TEST-CASES.md`
   - New package → update `ARCHITECTURE.md` (Section 9)
   - Security decision change → update `ARCHITECTURE.md` (Section 4)

3. **No doc debt allowed** — Code and docs must stay in sync. A change is not complete until the docs reflect it.

**When I ask for a change, Claude automatically:**
- Identifies which docs are affected
- Updates the code/design/config
- Updates all affected docs
- Adds a CHANGELOG entry
- Confirms what was changed and what docs were updated

---

## Project Overview

**TaskPilot** is a personal productivity web app on .NET 10 (ASP.NET Core Razor Pages, server-rendered).

| Attribute | Value |
|-----------|-------|
| Working directory | `C:\projects\TaskPilot` (Windows). **Launch Claude from this directory** so the project-scoped agents in `.claude/agents/` (architect, fullstack-dev, qa-engineer, ux-designer) are discovered. |
| Current iteration | **1** — Local dev, SQLite, dotnet user-secrets, dotnet run |
| Iteration 2 target | Azure App Service, Azure SQL/PostgreSQL, Key Vault, App Insights, GitHub Actions |
| Runtime | .NET 10, C# 13 |
| Frontend | ASP.NET Core Razor Pages + htmx + Bootstrap 5 + ApexCharts (server-rendered, no SPA build pipeline) |
| Backend | ASP.NET Core 10 Web API (controllers under `/api/v1/`) |
| Database | SQLite (iter 1), Azure SQL / PostgreSQL (iter 2) |
| Auth | ASP.NET Core Identity (cookie) + custom API key handler |
| Charts | ApexCharts (vanilla JS, loaded from `/lib/apexcharts/`) |
| Testing | xUnit (unit + integration via WebApplicationFactory) + Playwright for .NET (E2E) |

---

## Solution Structure (quick reference)

```
TaskPilot/
├── src/                              Single ASP.NET Core project (flat layout, NOT split server/client/shared)
│   ├── Pages/                        Razor Pages (.cshtml + .cshtml.cs page models)
│   ├── Controllers/                  /api/v1 REST controllers (Tasks, Tags, ApiKeys, Audit, Health, ActivityLog)
│   ├── Services/, Repositories/      Business logic + EF Core data access
│   ├── Entities/, Models/            EF entities + request/response DTOs + validators
│   ├── Mcp/                          MCP server tooling (LLM-callable)
│   ├── Data/                         ApplicationDbContext + per-entity Configurations
│   ├── Middleware/, Auth/            Audit middleware + API-key auth handler
│   └── wwwroot/                      Static assets (Bootstrap, htmx, ApexCharts, favicon, css/app.css)
└── tests/
    ├── TaskPilot.Tests.Unit/        xUnit + Moq + in-memory EF
    ├── TaskPilot.Tests.Integration/ xUnit + WebApplicationFactory + SQLite
    └── TaskPilot.Tests.E2E/         Playwright for .NET (runs against http://localhost:5125)
```

Full annotated directory tree: see [ARCHITECTURE.md §1](./ARCHITECTURE.md#1-solution-structure)

---

## Non-Negotiable Rules (summary)

Full rules in [REQUIREMENTS.md](./REQUIREMENTS.md#constraints). Key rules:

1. **No business logic in controllers** — Controllers: validate → call service → return response.
2. **No hard deletes** — Always `IsDeleted = true` + `DeletedAt`.
3. **No SQLite-specific features** — Must run on SQLite, Azure SQL, and PostgreSQL identically.
4. **No secrets in code** — `IConfiguration` only. `dotnet user-secrets` locally, Key Vault in prod.
5. **All API endpoints under `/api/v1/`**.
6. **Standard response envelope on all API responses** — no bare JSON.
7. **DTOs and enums in `src/Models/`** — namespaced under `TaskPilot.Models.*`.
8. **No rate limiting in iteration 1** — document insertion point only.
9. **`net10.0`** in all `.csproj` files.
10. **`LastModifiedBy`**: `"user:{username}"` (web UI) or `"api:{apiKeyName}"` (API).

---

## Architecture Patterns (quick reference)

Full patterns with code snippets: see [ARCHITECTURE.md §5](./ARCHITECTURE.md#5-coding-standards--patterns)

**Layering (strict):** `Controller → Service → Repository → DbContext`

**BaseEntity:** All entities inherit `Id`, `CreatedDate`, `LastModifiedDate`, `LastModifiedBy`.

**Response envelope:** See [ARCHITECTURE.md §3.5](./ARCHITECTURE.md#35-response-envelope-types)

**Auth:** Cookie for UI, `X-Api-Key` header for REST API. Keys hashed with HMAC-SHA256, stored as hash + 8-char prefix.

---

## Agent Authority Matrix

| Decision Domain | Authoritative Agent | Key Document |
|----------------|---------------------|--------------|
| Technical architecture, DB schema, packages | `architect` | ARCHITECTURE.md |
| Visual design, UX patterns, component library | `ux-designer` | DESIGN-SYSTEM.md, WIREFRAMES.md, USER-FLOWS.md |
| Implementation (backend + frontend code) | `fullstack-dev` | ARCHITECTURE.md + all design docs |
| Tests, security validation | `qa-engineer` | TEST-CASES.md, SECURITY-VALIDATION.md |

Agents must NOT make decisions in another agent's domain without coordination.
When in doubt about a decision, flag it to the orchestrator rather than improvising.

---

## Build Phases

| Phase | Description | Status |
|-------|-------------|--------|
| Phase 1 | Architecture + Design docs | ✅ Complete — awaiting Checkpoint 1 review |
| Phase 2 | Scaffolding + full backend (entities, DbContext, repos, services, controllers, auth, middleware) | ✅ Complete |
| Phase 3 | Frontend implementation (all pages, components, HTTP services) | ✅ Complete |
| Phase 4 | Testing + Security validation (73 tests passing) | ✅ Complete |
| Phase 5 | Polish + README + git init | ✅ Complete |

---

## Coding Standards (quick reference)

Full standards: see [ARCHITECTURE.md §5](./ARCHITECTURE.md#5-coding-standards--patterns)

| Element | Convention |
|---------|------------|
| Public members | PascalCase |
| Private fields | `_camelCase` |
| Local variables | `camelCase` |
| Async methods | Suffix `Async` |
| Test methods | `MethodName_Scenario_ExpectedResult` |
| Interfaces | `I` prefix |
| DTOs | `Request` / `Response` suffix |

- Methods < 30 lines
- One component per `.razor` file; extract if > 150 lines
- No `.Result` or `.Wait()`
- `ILogger<T>` with structured log messages
- `required` keyword + nullable reference types throughout
