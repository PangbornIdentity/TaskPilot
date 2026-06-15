# CVS — Defects & Open Questions

> Working artifact produced during the CVS bootstrap (Phase P1.5 — latent-requirement extraction
> from `src/`). These are **likely bugs and unresolved conflicts**, NOT requirements — they are
> deliberately kept out of `docs/requirements/**` so the bug behavior is never canonized as intent.
> Each was adversarially verified against the actual call path before being recorded here.
>
> Triage owner: human + `architect`. Resolution paths: fix the code (then co-update the affected
> record), open a tracked issue, or — for an "intended" finding — ratify the behavior into a record
> during P2/P6. Cross-cutting hard-delete questions need an `architect` decision.
>
> Created: 2026-06-14 (P1.5).

---

## A. Confirmed defects

### D-001 — `PATCH /api/v1/tasks/{id}` performs no request validation — Medium
- **Where:** `src/Controllers/TasksController.cs:64-70` → `src/Services/TaskService.cs:107` (`PatchTaskAsync`).
- **What:** POST (`CreateTaskRequestValidator`) and PUT (`UpdateTaskRequestValidator`) validate input; PATCH calls the service directly. No `IValidator<PatchTaskRequest>` is injected or invoked, and none exists in `src/Models/Validators/`. A client can PATCH `Title` to empty/>200 chars, or set `IsRecurring=true` with `RecurrencePattern=null`, reaching states the create/update validators forbid.
- **Proof:** `PatchTaskAsync` blindly assigns each supplied field (TaskService.cs:119-129); contrast CreateTask (validate at controller) / UpdateTask.
- **Fix sketch:** add a `PatchTaskRequest` validator (rules conditional on supplied fields) and gate the PATCH action on it.

### D-002 — Full task UPDATE under-logs field changes — Low–Medium
- **Where:** `src/Services/TaskService.cs:238-257` (`BuildActivityLogs`, full-update path).
- **What:** A full UPDATE logs Title, Description, TaskTypeId, Area, Priority, Status, TargetDate — but `TargetDateType`, `IsRecurring`, and `RecurrencePattern` are persisted (assigned at TaskService.cs:85-88) **without** an activity-log entry. PATCH *does* log all three. Audit history is silently incomplete for full updates and inconsistent with PATCH.
- **Fix sketch:** add the three missing fields to the full-update `BuildActivityLogs` diff.

### D-003 — Tag deletion is a HARD delete (violates Non-Negotiable Rule #2) — High *(see Q-001)*
- **Where:** `src/Controllers/TagsController.cs:68` → `src/Services/TagService.cs:58-66` → `src/Repositories/GenericRepository.cs:29-30` (`DbSet.Remove` + `SaveChangesAsync`).
- **What:** CLAUDE.md rule #2 mandates "No hard deletes — always `IsDeleted=true` + `DeletedAt`." `Tag` has no `IsDeleted`/`DeletedAt` fields and no soft-delete query filter, so deletion emits SQL `DELETE`. Only `TaskItem` soft-deletes.
- **Open question:** is rule #2 universal or scoped to `TaskItem`? See **Q-001** — needs an `architect` ruling before this is "fix" vs "ratify".
- **RESOLVED 2026-06-14**: Soft-delete shipped for `Tag` (WI-SOFTDELETE) — `Tag` gained `IsDeleted`/`DeletedAt` + global query filter; `TagService` now soft-deletes instead of `Remove()`. Record co-updated (FR-TAGS-004, NFR-DATA-001 → "soft for every deletable entity"). Covered by `TagSoftDeleteTests` + `TagSoftDeleteIntegrationTests`.

### D-004 — Tag delete cascades and permanently drops `TaskTag` join rows — Medium
- **Where:** `src/Data/Configurations/TagConfiguration.cs:20-23` (`OnDelete(DeleteBehavior.Cascade)`).
- **What:** Compounds D-003 — hard-deleting a tag permanently removes every task↔tag association with no recovery path. A soft-delete remediation for D-003 must account for the join table.
- **RESOLVED 2026-06-14**: Soft-delete shipped (WI-SOFTDELETE) — a soft-deleted tag no longer cascades; its `TaskTag` join rows are preserved (the tag is simply filtered out of reads). Covered by `TagSoftDeleteTests` + `TagSoftDeleteIntegrationTests`; record FR-TAGS-004 states associations are preserved.

### D-005 — `PATCH /api/v1/apikeys/{id}/rename` performs no input validation — Low–Medium
- **Where:** `src/Controllers/ApiKeysController.cs:33-39` → `src/Services/ApiKeyService.cs:40-49` (`RenameKeyAsync`).
- **What:** Key creation enforces non-empty + ≤100 chars (`CreateApiKeyRequestValidator`); rename invokes no validator (no `RenameApiKeyRequest` validator class exists) and assigns `key.Name = request.Name` unchecked. Empty or >100-char names are accepted via rename. (Ownership check at ApiKeyService.cs:43 is present — not a security hole, a consistency gap.)
- **Fix sketch:** add a `RenameApiKeyRequest` validator mirroring the create name rules; gate the rename action.

### D-006 — Tasks page "completed"/"all" scopes under-count (paginate-then-filter) — Medium
- **Where:** `src/Pages/Tasks/Index.cshtml.cs:97-123`.
- **What:** For `show=completed` (and `all`) the page requests a server-paginated page (`PageSize:50`, `IncludeOnlyIncomplete:false`, no terminal-status filter pushed to the repo) and then post-filters the returned 50 rows down to Completed/Cancelled at the page layer. The repository applies `Skip/Take` server-side (`TaskRepository.cs:129-130`), so the 50-row page already mixes all statuses; post-filtering yields fewer than a full page of completed rows and loses completed tasks beyond the first 50 mixed rows. `TotalCount = Tasks.Count` then reports only the filtered slice — completed/all views silently under-count.
- **Fix sketch:** push the terminal-status filter into the repository query for the completed/all scopes so pagination and counts are computed server-side.

### D-007 — API-key privilege escalation: an API key can manage API keys (LDG-007) — High
- **Where:** `src/Controllers/ApiKeysController.cs` → multi-scheme selector in `ServiceCollectionExtensions` (forwards to the ApiKey scheme whenever `X-Api-Key` is present).
- **What:** The API-key management endpoints did not pin the cookie authentication scheme, so a request bearing only `X-Api-Key` could list/create/rename/activate/deactivate/revoke API keys — letting a lower-trust automation credential escalate to manage the credential set itself.
- **RESOLVED 2026-06-14**: Cookie (session) scheme pinned on `ApiKeysController`; an `X-Api-Key`-only request to `/api/v1/apikeys` is now rejected with 401. Ratified as **NFR-SEC-010**, covered by `ApiKeySchemeRestrictionTests`. (Ledger row LDG-007.)

---

## B. Open questions / escalations (feed the P2 reconciliation ledger)

### Q-001 — Scope of the "no hard deletes" rule (Tag, ApiKey) — needs `architect` decision
- `ApiKey` revoke also hard-deletes (`ApiKeyService.cs:62-70`), **but** `REQUIREMENTS.md:257` documents key revoke as "permanent" — so the ApiKey hard-delete appears **intentional** (a documented carve-out). `Tag` hard-delete (D-003) is **undocumented**.
- **Decision needed:** is rule #2 universal (then Tag — and arguably ApiKey — must gain `IsDeleted`/`DeletedAt`) or explicitly scoped to `TaskItem` (then amend CLAUDE.md/REQUIREMENTS.md to say so, ratify the Tag/ApiKey behavior, and close D-003)?
- **RESOLVED 2026-06-14**: Decided **universal — no hard deletes anywhere** (LDG-001). Both `Tag` and `ApiKey` now soft-delete (`IsDeleted`/`DeletedAt` + global query filter); the prior "permanent" ApiKey carve-out is superseded — revoke is now a recoverable soft-delete with `IsActive=false`. NFR-DATA-001 reworded to "every deletable entity (TaskItem, Tag, ApiKey)". Closes D-003/D-004.

### Q-002 — `NFR-DATA-003` (audit-write) is real-but-untested + under-specified
- Adversarial trace confirmed `ApiAuditMiddleware` **does** persist an audit row, but only for requests that are (1) `/api/v1/*`, (2) **not** `/api/v1/health`, and (3) authenticated via `X-Api-Key` (the `AuthenticationMethod=ApiKey` claim). Cookie/UI requests are deliberately **not** audited. Captured fields include method, endpoint, status, duration, api-key id/name, user id, and a SHA-256 body hash.
- **Recommended P2/P6 wording** (do not edit the record until reconciled): *"Every request authenticated via `X-Api-Key` to a non-health `/api/v1/*` endpoint is recorded to the API audit log with HTTP method, endpoint, status code, duration, resolving API-key id/name, user id, and a SHA-256 hash of the request body; health endpoints and cookie/UI-authenticated requests are not audited."*
- **Action:** add an integration test that issues an API-key request and asserts a persisted `ApiAuditLog` row (closes the `tests: []` gap), then ratify.

### Q-003 — Recurring successor with no target date spawns a date-less successor — Low
- **Where:** `src/Services/TaskService.cs:209-215`. A `IsRecurring` task whose `TargetDate` is null produces a successor with `TargetDate = source.TargetDate?.AddX()` = null, indefinitely. No validator ties `IsRecurring` to a target date.
- **Decision needed:** must recurring tasks have a target date? If yes → add a validator rule (and it's a defect). If no → intended (already encoded in `BIZ-TASKS-029`'s null clause).

### Q-004 — Stats week-label ordering may be lexicographic / non-ISO — Low
- **Where:** `src/Services/StatsService.cs` (CompletedPerWeek / CompletionRate / AvgCompletion). Week buckets use `(DayOfYear-1)/7` (not ISO-8601), and labels are sorted with `OrderBy(x => x.WeekLabel)` on strings like `"W10/2026"` vs `"W2/2026"` — which orders `W10` before `W2`. This is the same bug class already fixed in `ChangelogService`'s SemVer sort.
- **Decision needed:** confirm whether the rendered chart re-sorts client-side; if not, this is a real ordering defect.

### Q-005 — Registration cookie persistence inconsistency — Low
- API register signs in with `isPersistent: false` (`AccountController.cs:26`) while web register uses `isPersistent: true` (`Register.cshtml.cs:26`); API login uses `true` (`AccountController.cs:33`). Likely benign (API clients use API keys, not cookies) but worth a one-line product confirmation.
