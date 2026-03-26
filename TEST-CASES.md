# TaskPilot — Test Cases

> Complete test case specification for all testing layers.
> Test code lives in `tests/`. This document is the specification that code implements.
> **Owner:** `qa-engineer` agent
> **Updated:** Whenever new features are added or existing behavior changes.

---

## Table of Contents
1. [Unit Tests — Services](#1-unit-tests--services)
2. [Unit Tests — Validators](#2-unit-tests--validators)
3. [Unit Tests — Repositories](#3-unit-tests--repositories)
4. [Unit Tests — bUnit Component Tests](#4-unit-tests--bunit-component-tests)
5. [Integration Tests — Authentication](#5-integration-tests--authentication)
6. [Integration Tests — Task Endpoints](#6-integration-tests--task-endpoints)
7. [Integration Tests — Tag Endpoints](#7-integration-tests--tag-endpoints)
8. [Integration Tests — API Key Endpoints](#8-integration-tests--api-key-endpoints)
9. [Integration Tests — Audit Endpoints](#9-integration-tests--audit-endpoints)
10. [Integration Tests — Stats Endpoints](#10-integration-tests--stats-endpoints)
11. [Integration Tests — Middleware & Pipeline](#11-integration-tests--middleware--pipeline)
12. [Playwright E2E Tests](#12-playwright-e2e-tests)
13. [Security Validation Tests](#13-security-validation-tests)

**Naming convention:** `MethodName_Scenario_ExpectedResult`
**Pattern:** Arrange–Act–Assert, one logical assertion per test

---

## 1. Unit Tests — Services

**Project:** `TaskPilot.Tests.Unit/Services/`

### 1.1 TaskService

| # | Test Name | Arrange | Act | Assert |
|---|-----------|---------|-----|--------|
| U-T-001 | `CreateTaskAsync_WithValidRequest_ReturnsCreatedTask` | Valid `CreateTaskRequest`, mocked repo | `CreateTaskAsync` | Returns `TaskResponse` with matching fields, `Status = NotStarted` |
| U-T-002 | `CreateTaskAsync_WithValidRequest_SetsLastModifiedByToUser` | Valid request, `userId = "user:alice"` | `CreateTaskAsync` | Returned task has `LastModifiedBy = "user:alice"` |
| U-T-003 | `CreateTaskAsync_WithRecurring_SetsRecurrencePattern` | Request with `IsRecurring=true`, `RecurrencePattern=Weekly` | `CreateTaskAsync` | Returned task has `IsRecurring=true`, `RecurrencePattern=Weekly` |
| U-T-004 | `CreateTaskAsync_WithoutRecurring_NullRecurrencePattern` | Request with `IsRecurring=false` | `CreateTaskAsync` | `RecurrencePattern = null` |
| U-T-005 | `CompleteTaskAsync_NonRecurringTask_SetsCompletedDate` | Task exists, `IsRecurring=false` | `CompleteTaskAsync` | `Status=Completed`, `CompletedDate` set to approximately now |
| U-T-006 | `CompleteTaskAsync_WithResultAnalysis_SavesAnalysis` | Task exists, request has `ResultAnalysis="text"` | `CompleteTaskAsync` | `ResultAnalysis = "text"` |
| U-T-007 | `CompleteTaskAsync_WithRecurringWeekly_CreatesNextInstance` | Task with `IsRecurring=true`, `RecurrencePattern=Weekly`, `TargetDate=Monday` | `CompleteTaskAsync` | New task created with same title/priority, `TargetDate = Monday+7` |
| U-T-008 | `CompleteTaskAsync_WithRecurringMonthly_CreatesNextMonthInstance` | Task with `RecurrencePattern=Monthly`, `TargetDate=Jan 15` | `CompleteTaskAsync` | New task `TargetDate = Feb 15` |
| U-T-009 | `CompleteTaskAsync_AlreadyCompleted_ThrowsConflictException` | Task with `Status=Completed` | `CompleteTaskAsync` | Throws `ConflictException` (or equivalent — 409) |
| U-T-010 | `CompleteTaskAsync_WritesActivityLogEntry` | Task exists | `CompleteTaskAsync` | `TaskActivityLog` entry written: `FieldChanged="Status"`, `OldValue="InProgress"`, `NewValue="Completed"` |
| U-T-011 | `UpdateTaskAsync_ChangesStatus_WritesActivityLog` | Task with `Status=NotStarted`, update request sets `Status=InProgress` | `UpdateTaskAsync` | Activity log entry for `FieldChanged="Status"` |
| U-T-012 | `UpdateTaskAsync_SetsLastModifiedDateToNow` | Task exists | `UpdateTaskAsync` | `LastModifiedDate` is approximately `DateTime.UtcNow` |
| U-T-013 | `SoftDeleteTaskAsync_SetsIsDeletedTrue` | Task exists | `SoftDeleteTaskAsync` | `IsDeleted=true`, `DeletedAt` set |
| U-T-014 | `SoftDeleteTaskAsync_TaskDoesNotExist_ThrowsNotFoundException` | No task with given ID | `SoftDeleteTaskAsync` | Throws `NotFoundException` |
| U-T-015 | `GetTaskByIdAsync_ExcludesSoftDeleted` | Task with `IsDeleted=true` | `GetTaskByIdAsync` | Returns null / throws `NotFoundException` |
| U-T-016 | `GetTaskByIdAsync_WrongUser_ThrowsNotFoundException` | Task exists for user A | Call with user B's ID | Throws `NotFoundException` (no cross-user leakage) |
| U-T-017 | `CreateTaskAsync_SetsCreatedDateToUtcNow` | Valid request | `CreateTaskAsync` | `CreatedDate` within 1 second of `DateTime.UtcNow` |
| U-T-018 | `UpdateTaskAsync_ApiCaller_SetsLastModifiedByToApiKey` | Valid request, caller is API key "Claude-Work" | `UpdateTaskAsync` | `LastModifiedBy = "api:Claude-Work"` |

### 1.2 TagService

| # | Test Name | Scenario | Expected |
|---|-----------|----------|----------|
| U-TAG-001 | `CreateTagAsync_WithValidName_ReturnsTag` | Name "bug", color "#EF4444" | Tag returned with matching fields |
| U-TAG-002 | `CreateTagAsync_DuplicateNameForUser_ThrowsConflictException` | Tag "bug" already exists for user | Throws `ConflictException` |
| U-TAG-003 | `CreateTagAsync_SameNameDifferentUser_Succeeds` | Tag "bug" exists for user A; user B creates same name | Succeeds (names are scoped per user) |
| U-TAG-004 | `DeleteTagAsync_RemovesFromAllTasks` | Tag assigned to 3 tasks | After delete, all `TaskTag` entries removed |

### 1.3 ApiKeyService

| # | Test Name | Scenario | Expected |
|---|-----------|----------|----------|
| U-AK-001 | `GenerateApiKeyAsync_ReturnsPlaintextKeyOnce` | Valid request | Returns `GeneratedApiKeyResponse` with `FullKey` (non-null, non-empty) |
| U-AK-002 | `GenerateApiKeyAsync_StoredKeyIsHashed` | Generate key | `ApiKey.KeyHash` != plaintext key |
| U-AK-003 | `GenerateApiKeyAsync_StoresPrefix` | Generate key with plaintext "tp_xK9mR2bv..." | `KeyPrefix = "tp_xK9mR2"` (first 8 chars) |
| U-AK-004 | `ValidateApiKeyAsync_ValidActiveKey_ReturnsKeyInfo` | Valid key, `IsActive=true` | Returns key details including name and user ID |
| U-AK-005 | `ValidateApiKeyAsync_InactiveKey_ReturnsNull` | Key with `IsActive=false` | Returns null |
| U-AK-006 | `ValidateApiKeyAsync_WrongKey_ReturnsNull` | Non-existent key hash | Returns null |
| U-AK-007 | `DeactivateApiKeyAsync_SetsIsActiveFalse` | Active key | `IsActive = false` |
| U-AK-008 | `RevokeApiKeyAsync_DeletesKey` | Active key | Key no longer in repository |

### 1.4 StatsService

| # | Test Name | Scenario | Expected |
|---|-----------|----------|----------|
| U-ST-001 | `GetStatsAsync_CountsActiveTasks` | 5 NotStarted + 3 InProgress (not deleted) | `ActiveCount = 8` |
| U-ST-002 | `GetStatsAsync_ExcludesSoftDeletedFromCounts` | 3 tasks, 1 soft-deleted | Counts exclude soft-deleted |
| U-ST-003 | `GetCompletionsByWeekAsync_ReturnsLast12Weeks` | Tasks completed over 14 weeks | Returns array of 12 entries |
| U-ST-004 | `GetStatsAsync_CompletedTodayReflectsToday` | Task completed 3 hours ago, another completed yesterday | `CompletedToday = 1` |
| U-ST-005 | `GetStatsAsync_OverdueIsCorrectlyCalculated` | Task with `TargetDate = yesterday`, `Status = InProgress` | Counted as overdue |

---

## 2. Unit Tests — Validators

**Project:** `TaskPilot.Tests.Unit/Validators/`

### 2.1 CreateTaskRequestValidator

| # | Test Name | Input | Expected |
|---|-----------|-------|----------|
| U-V-001 | `Validate_EmptyTitle_FailsWithMessage` | `Title = ""` | Invalid, error on `Title`: "Title is required" |
| U-V-002 | `Validate_TitleExceeds200Chars_FailsWithMessage` | `Title` = 201-char string | Invalid, error on `Title`: "Title must not exceed 200 characters" |
| U-V-003 | `Validate_ValidTitle_Passes` | `Title = "Valid task"` | Valid |
| U-V-004 | `Validate_InvalidPriority_FailsWithMessage` | `Priority = 999` | Invalid, error on `Priority` |
| U-V-005 | `Validate_RecurringWithoutPattern_FailsWithMessage` | `IsRecurring=true`, `RecurrencePattern=null` | Invalid: "RecurrencePattern is required when IsRecurring is true" |
| U-V-006 | `Validate_RecurringWithPattern_Passes` | `IsRecurring=true`, `RecurrencePattern=Weekly` | Valid |
| U-V-007 | `Validate_NotRecurringWithPattern_FailsWithMessage` | `IsRecurring=false`, `RecurrencePattern=Weekly` | Invalid: "RecurrencePattern must be null when IsRecurring is false" |
| U-V-008 | `Validate_SpecificDayWithoutDate_FailsWithMessage` | `TargetDateType=SpecificDay`, `TargetDate=null` | Invalid: "TargetDate is required for SpecificDay" |
| U-V-009 | `Validate_SpecificDayWithDate_Passes` | `TargetDateType=SpecificDay`, `TargetDate=tomorrow` | Valid |
| U-V-010 | `Validate_NullDescription_Passes` | `Description = null` | Valid (optional) |

### 2.2 GenerateApiKeyRequestValidator

| # | Test Name | Input | Expected |
|---|-----------|-------|----------|
| U-V-011 | `Validate_EmptyName_FailsWithMessage` | `Name = ""` | Invalid: "Name is required" |
| U-V-012 | `Validate_NameExceeds100Chars_FailsWithMessage` | `Name` = 101-char string | Invalid: "Name must not exceed 100 characters" |
| U-V-013 | `Validate_ValidName_Passes` | `Name = "Claude-Work"` | Valid |

---

## 3. Unit Tests — Repositories

**Project:** `TaskPilot.Tests.Unit/Repositories/`
**Setup:** SQLite in-memory provider (fresh DB per test class)

| # | Test Name | Setup | Act | Assert |
|---|-----------|-------|-----|--------|
| U-R-001 | `GetFilteredAsync_ByStatus_ReturnsOnlyMatchingStatus` | 5 tasks: 3 InProgress, 2 NotStarted | Filter `Status=InProgress` | Returns exactly 3 tasks |
| U-R-002 | `GetFilteredAsync_ByPriority_ReturnsCorrectTasks` | 3 High + 2 Low tasks | Filter `Priority=High` | Returns 3 |
| U-R-003 | `GetFilteredAsync_ExcludesSoftDeleted` | 3 tasks, 1 soft-deleted | No filter | Returns 2 |
| U-R-004 | `GetFilteredAsync_SearchTitle_CaseInsensitive` | Task titled "Fix Login Bug" | `search="login"` | Returns 1 |
| U-R-005 | `GetFilteredAsync_Pagination_ReturnsCorrectPage` | 25 tasks | Page 2, PageSize 10 | Returns tasks 11–20, `TotalCount=25`, `TotalPages=3` |
| U-R-006 | `GetFilteredAsync_SortByPriorityAsc_ReturnsCriticalFirst` | 3 tasks: Low, Critical, Medium | Sort Priority ASC | Critical first |
| U-R-007 | `GetFilteredAsync_ByTag_ReturnsOnlyTaggedTasks` | 5 tasks, 2 tagged with "bug" | Filter `tags=bugTagId` | Returns 2 |
| U-R-008 | `GetFilteredAsync_ByUserid_ReturnsOnlyUsersTasks` | 3 tasks for user A, 2 for user B | Filter by user A | Returns 3 |
| U-R-009 | `GetFilteredAsync_ByDateRange_ReturnsTasksInRange` | Tasks with TargetDates spanning 3 weeks | Filter last 7 days | Returns only tasks in range |
| U-R-010 | `GetWithTagsAsync_IncludesTagObjects` | Task with 2 tags | `GetWithTagsAsync` | Returned task has `Tags` populated |

---

## 4. Unit Tests — bUnit Component Tests

**Project:** `TaskPilot.Tests.Unit/Components/`

| # | Test Name | Component | Scenario | Expected |
|---|-----------|-----------|----------|----------|
| U-C-001 | `TaskCard_CriticalPriority_ShowsRedBadge` | `TaskCard` | Priority=Critical | Priority badge has CSS class for red/error color |
| U-C-002 | `TaskCard_CompletedStatus_ShowsGreenBadge` | `TaskCard` | Status=Completed | Status badge has CSS class for green/success |
| U-C-003 | `TaskCard_NullTargetDate_DoesNotThrow` | `TaskCard` | `TargetDate=null` | Renders without exception, no date shown |
| U-C-004 | `TaskCard_OverdueDate_ShowsRedDate` | `TaskCard` | `TargetDate=yesterday`, `Status=InProgress` | Date text has error color class |
| U-C-005 | `PriorityBadge_AllPriorities_RenderCorrectly` | `PriorityBadge` | Render for each of 4 priorities | Each renders without exception, correct label text |
| U-C-006 | `StatusBadge_AllStatuses_RenderCorrectly` | `StatusBadge` | Render for each of 5 statuses | Each renders without exception, correct label text |
| U-C-007 | `EmptyState_RendersHeadingAndCta` | `EmptyState` | Pass heading + CTA text as params | Heading and CTA button visible in render |
| U-C-008 | `SkeletonLoader_RendersWithoutData` | `SkeletonLoader` | Render with no data | Skeleton elements present in output |
| U-C-009 | `ToastContainer_Success_ShowsGreenToast` | `ToastContainer` | Service fires success toast | Toast element has success color class, correct message |
| U-C-010 | `ToastContainer_UndoToast_ShowsUndoButton` | `ToastContainer` | Undo toast triggered | Undo button present in toast |
| U-C-011 | `AppSidebar_AuthenticatedUser_ShowsNavLinks` | `AppSidebar` | Auth state = logged in | Dashboard, Tasks, Audit, Settings links rendered |
| U-C-012 | `AppSidebar_UnauthenticatedUser_DoesNotShowNavLinks` | `AppSidebar` | Auth state = logged out | Nav links not rendered |

---

## 5. Integration Tests — Authentication

**Project:** `TaskPilot.Tests.Integration/Auth/`
**Setup:** `WebApplicationFactory<Program>` with fresh SQLite in-memory DB

| # | Test Name | Setup | Request | Expected |
|---|-----------|-------|---------|----------|
| I-A-001 | `Register_ValidCredentials_Returns200AndSetsCookie` | No existing user | POST `/auth/register` with valid body | 200, auth cookie set |
| I-A-002 | `Register_DuplicateEmail_Returns409WithEnvelope` | User already exists | POST `/auth/register` same email | 409, error envelope with `code="CONFLICT"` |
| I-A-003 | `Register_WeakPassword_Returns400WithValidationErrors` | — | POST `/auth/register`, password "abc" | 400, `details` includes password field error |
| I-A-004 | `Login_ValidCredentials_Returns200AndSetsCookie` | User exists | POST `/auth/login` | 200, auth cookie set |
| I-A-005 | `Login_WrongPassword_Returns401` | User exists | POST `/auth/login` wrong password | 401, error envelope |
| I-A-006 | `Login_NonExistentUser_Returns401` | No user | POST `/auth/login` | 401 (same response — no user enumeration) |
| I-A-007 | `ProtectedRoute_WithoutCookie_Returns401` | — | GET `/api/v1/tasks` no auth | 401, error envelope |
| I-A-008 | `ProtectedRoute_WithCookie_Returns200` | Logged-in client | GET `/api/v1/tasks` with cookie | 200 |
| I-A-009 | `ApiKeyAuth_ValidActiveKey_Returns200` | User + active API key | GET `/api/v1/tasks` with `X-Api-Key: <valid>` | 200 |
| I-A-010 | `ApiKeyAuth_InvalidKey_Returns401` | — | GET `/api/v1/tasks` with `X-Api-Key: badkey` | 401, error envelope |
| I-A-011 | `ApiKeyAuth_InactiveKey_Returns401` | User + deactivated key | GET `/api/v1/tasks` with inactive key | 401 |
| I-A-012 | `ApiKeyAuth_MissingHeader_Returns401` | — | GET `/api/v1/tasks` no header | 401 |
| I-A-013 | `ApiKeyAuth_CreatesAuditLogEntry` | User + API key | GET `/api/v1/tasks` with key | `ApiAuditLog` has 1 new entry with correct method/endpoint/status |
| I-A-014 | `CookieAuth_DoesNotCreateAuditLog` | Logged-in client | GET `/api/v1/tasks` with cookie | No new `ApiAuditLog` entry created |

---

## 6. Integration Tests — Task Endpoints

**Project:** `TaskPilot.Tests.Integration/Api/TasksEndpointTests.cs`

### GET /api/v1/tasks

| # | Test Name | Setup | Request | Expected |
|---|-----------|-------|---------|----------|
| I-T-001 | `GetTasks_Authenticated_Returns200WithEnvelope` | 3 tasks in DB | GET `/api/v1/tasks` | 200, `data` array, `meta.totalCount=3` |
| I-T-002 | `GetTasks_FilterByStatus_ReturnsFiltered` | 3 InProgress + 2 NotStarted | GET `?status=InProgress` | `data` has 3 items |
| I-T-003 | `GetTasks_FilterByPriority_ReturnsFiltered` | 2 High + 3 Low | GET `?priority=High` | `data` has 2 items |
| I-T-004 | `GetTasks_SearchByTitle_ReturnsMatching` | Tasks with various titles | GET `?search=login` | Only tasks with "login" in title/description |
| I-T-005 | `GetTasks_Pagination_ReturnsCorrectPage` | 25 tasks | GET `?page=2&pageSize=10` | 10 items, `meta.page=2`, `meta.totalPages=3` |
| I-T-006 | `GetTasks_ExcludesSoftDeleted` | 3 tasks, 1 soft-deleted | GET `/api/v1/tasks` | Only 2 tasks returned |
| I-T-007 | `GetTasks_Unauthenticated_Returns401` | — | GET `/api/v1/tasks` no auth | 401, error envelope |

### GET /api/v1/tasks/{id}

| # | Test Name | Expected |
|---|-----------|----------|
| I-T-008 | `GetTaskById_ValidId_Returns200WithTask` | 200, `data.id` matches |
| I-T-009 | `GetTaskById_NotFound_Returns404WithEnvelope` | 404, `error.code = "NOT_FOUND"` |
| I-T-010 | `GetTaskById_OtherUsersTask_Returns404` | 404 (no cross-user data leak) |
| I-T-011 | `GetTaskById_SoftDeleted_Returns404` | 404 |

### POST /api/v1/tasks

| # | Test Name | Request Body | Expected |
|---|-----------|-------------|----------|
| I-T-012 | `CreateTask_ValidBody_Returns201WithCreatedTask` | Valid `CreateTaskRequest` | 201, `data` has all fields, `Location` header set |
| I-T-013 | `CreateTask_EmptyTitle_Returns400WithValidationErrors` | `title: ""` | 400, `error.details` contains title error |
| I-T-014 | `CreateTask_TitleOver200Chars_Returns400` | 201-char title | 400 |
| I-T-015 | `CreateTask_SetsLastModifiedByToUser` | Valid request, cookie auth | `data.lastModifiedBy = "user:testuser"` |
| I-T-016 | `CreateTask_ViaApiKey_SetsLastModifiedByToApiKeyName` | Valid request, API key "Claude-Work" | `data.lastModifiedBy = "api:Claude-Work"` |
| I-T-017 | `CreateTask_Unauthenticated_Returns401` | Valid body, no auth | 401 |

### PUT /api/v1/tasks/{id}

| # | Test Name | Expected |
|---|-----------|----------|
| I-T-018 | `UpdateTask_ValidBody_Returns200WithUpdatedTask` | 200, all fields updated |
| I-T-019 | `UpdateTask_NotFound_Returns404` | 404 |
| I-T-020 | `UpdateTask_InvalidBody_Returns400` | 400, validation errors |
| I-T-021 | `UpdateTask_UpdatesLastModifiedDate` | `lastModifiedDate` > original |
| I-T-022 | `UpdateTask_WritesActivityLogEntries` | `TaskActivityLog` has entries for changed fields |

### PATCH /api/v1/tasks/{id}

| # | Test Name | Expected |
|---|-----------|----------|
| I-T-023 | `PatchTask_OnlyChangesSpecifiedFields` | `{ "status": "InProgress" }` — only status changes, all other fields unchanged |
| I-T-024 | `PatchTask_ChangesStatus_WritesActivityLog` | Activity log has Status change entry |

### DELETE /api/v1/tasks/{id}

| # | Test Name | Expected |
|---|-----------|----------|
| I-T-025 | `DeleteTask_ValidId_Returns204` | 204 No Content |
| I-T-026 | `DeleteTask_SetsIsDeletedTrueInDb` | DB record has `IsDeleted=true`, `DeletedAt` set |
| I-T-027 | `DeleteTask_DisappearsFromList` | After delete: GET `/api/v1/tasks` no longer includes it |
| I-T-028 | `DeleteTask_NotFound_Returns404` | 404 |
| I-T-029 | `DeleteTask_DoesNotHardDelete` | Row still exists in DB with `IsDeleted=true` |

### POST /api/v1/tasks/{id}/complete

| # | Test Name | Expected |
|---|-----------|----------|
| I-T-030 | `CompleteTask_ValidId_Returns200WithCompletedTask` | 200, `status=Completed`, `completedDate` set |
| I-T-031 | `CompleteTask_WithResultAnalysis_SavesAnalysis` | Body `{ "resultAnalysis": "..." }` → `resultAnalysis` in response |
| I-T-032 | `CompleteTask_AlreadyCompleted_Returns409` | 409, `error.code = "CONFLICT"` |
| I-T-033 | `CompleteTask_RecurringTask_CreatesNextInstance` | Recurring task → 200 + new task created |
| I-T-034 | `CompleteTask_NotFound_Returns404` | 404 |

### Task Lifecycle (end-to-end chain)

| # | Test Name | Steps |
|---|-----------|-------|
| I-T-035 | `TaskLifecycle_CreateEditComplete_AllFieldsCorrect` | POST create → verify fields → PUT update → verify `LastModifiedDate` changed → POST complete → verify `Status`, `CompletedDate`, `LastModifiedBy` at each step |

---

## 7. Integration Tests — Tag Endpoints

**Project:** `TaskPilot.Tests.Integration/Api/TagsEndpointTests.cs`

| # | Test Name | Expected |
|---|-----------|----------|
| I-TAG-001 | `GetTags_Returns200WithList` | 200, `data` array |
| I-TAG-002 | `CreateTag_ValidBody_Returns201` | 201, `data.name` matches |
| I-TAG-003 | `CreateTag_DuplicateName_Returns409` | 409 |
| I-TAG-004 | `DeleteTag_ValidId_Returns204` | 204 |
| I-TAG-005 | `DeleteTag_RemovesFromTasksInDb` | After delete, task no longer has tag |
| I-TAG-006 | `CreateTag_Unauthenticated_Returns401` | 401 |

---

## 8. Integration Tests — API Key Endpoints

**Project:** `TaskPilot.Tests.Integration/Api/ApiKeysEndpointTests.cs`

| # | Test Name | Expected |
|---|-----------|----------|
| I-AK-001 | `GenerateApiKey_ValidName_Returns201WithFullKey` | 201, `data.fullKey` non-null (plaintext, shown once) |
| I-AK-002 | `GenerateApiKey_StoredKeyIsHashedNotPlaintext` | DB `KeyHash` != `data.fullKey` |
| I-AK-003 | `GenerateApiKey_SubsequentGet_FullKeyNotVisible` | GET `/api/v1/apikeys` does NOT include `fullKey` in response |
| I-AK-004 | `ListApiKeys_ShowsPrefix_NotHash` | Response `data[].keyPrefix` is 8 chars, `data[].keyHash` not present |
| I-AK-005 | `DeactivateApiKey_Returns200_IsActiveFalse` | 200, `data.isActive=false` |
| I-AK-006 | `DeactivatedKey_CannotAuthenticate` | Deactivate key → use key → 401 |
| I-AK-007 | `RevokeApiKey_Returns204_KeyDeleted` | 204, key gone from DB |
| I-AK-008 | `ApiKeyEndpoints_RequireCookieAuth_NotApiKeyAuth` | Access with API key header (not cookie) → 401 |
| I-AK-009 | `GenerateApiKey_EmptyName_Returns400` | 400, validation error |

---

## 9. Integration Tests — Audit Endpoints

**Project:** `TaskPilot.Tests.Integration/Api/AuditEndpointTests.cs`

| # | Test Name | Expected |
|---|-----------|----------|
| I-AU-001 | `GetAuditLogs_Returns200WithPaginatedList` | 200, `data` array with `meta.totalCount` |
| I-AU-002 | `GetAuditLogs_After5ApiKeyRequests_Has5Entries` | Make 5 API-key requests → GET audit → `totalCount=5` |
| I-AU-003 | `GetAuditLogs_FilterByApiKey_ReturnsOnlyThatKey` | 2 API keys, 3 requests each → filter by key A → `totalCount=3` |
| I-AU-004 | `GetAuditLogs_FilterByDateRange_ReturnsCorrectRange` | Requests from today and yesterday → filter today only → correct count |
| I-AU-005 | `GetAuditLogs_EntriesHaveCorrectFields` | Make a POST request → check audit entry has `httpMethod=POST`, `endpoint`, `responseStatusCode`, `durationMs>0` |
| I-AU-006 | `AuditLogs_AreImmutable_NoWriteEndpointExists` | Attempt POST/PUT/DELETE `/api/v1/audit/{id}` → 404 or 405 |
| I-AU-007 | `AuditLogs_RequireCookieAuth` | GET `/api/v1/audit` with API key (not cookie) → 401 |
| I-AU-008 | `GetAuditLogs_Pagination_WorksCorrectly` | 25 audit entries → GET page 1 size 10 → 10 entries, correct meta |

---

## 10. Integration Tests — Stats Endpoints

**Project:** `TaskPilot.Tests.Integration/Api/StatsEndpointTests.cs`

| # | Test Name | Expected |
|---|-----------|----------|
| I-ST-001 | `GetStats_Returns200WithStatsResponse` | 200, `data` has `activeCount`, `completedToday`, `overdue`, etc. |
| I-ST-002 | `GetStats_CountsReflectActualData` | Seed known tasks → verify counts match |
| I-ST-003 | `GetStats_ExcludesSoftDeleted` | Soft-deleted tasks not counted in active |
| I-ST-004 | `GetStats_Unauthenticated_Returns401` | 401 |

---

## 11. Integration Tests — Middleware & Pipeline

**Project:** `TaskPilot.Tests.Integration/`

| # | Test Name | Expected |
|---|-----------|----------|
| I-MW-001 | `SecurityHeaders_ArePresent_OnAllResponses` | Response includes `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy` |
| I-MW-002 | `GlobalExceptionHandler_UnhandledException_ReturnsErrorEnvelope` | Trigger unhandled exception → 500, `error.code = "INTERNAL_ERROR"`, no stack trace |
| I-MW-003 | `GlobalExceptionHandler_ProductionMode_NoStackTrace` | With `ASPNETCORE_ENVIRONMENT=Production` → 500 error response has no exception details |
| I-MW-004 | `Swagger_IsAvailable_InDevelopment` | GET `/swagger/index.html` in dev → 200 |
| I-MW-005 | `Swagger_IsNotAvailable_InProduction` | With `ASPNETCORE_ENVIRONMENT=Production` → GET `/swagger` → 404 |
| I-MW-006 | `FluentValidation_RunsBeforeController_Returns400` | Invalid request body → 400 before controller action executes |
| I-MW-007 | `RequestId_PresentInAllResponses` | All responses have `meta.requestId` (non-empty GUID) |
| I-MW-008 | `CORS_AllowsConfiguredOrigin_BlocksOthers` | Request from allowed origin → CORS headers present; disallowed origin → no CORS headers |

---

## 12. Playwright E2E Tests

**Project:** `TaskPilot.Tests.E2E/`
**Pattern:** Page Object Model. One class per page.
**Tag:** All tests `[Trait("Category", "E2E")]`
**Run command:** `dotnet test --filter Category=E2E`

### Setup
```bash
# After building E2E project, install Playwright browsers (run once):
pwsh bin/Debug/net10.0/playwright.ps1 install
```

### 12.1 Auth Tests (`AuthTests.cs`)

| # | Test Name | Steps | Expected |
|---|-----------|-------|----------|
| E-A-001 | `Register_ValidCredentials_LogsInAndShowsDashboard` | Navigate to `/` → auto-redirect to `/login` → click Register tab → fill form → submit | Dashboard loads, welcome banner visible |
| E-A-002 | `Register_DuplicateEmail_ShowsError` | Register twice with same email | Error message visible on form |
| E-A-003 | `Login_ValidCredentials_NavigatesToDashboard` | `/login` → fill form → submit | Dashboard URL, user nav visible |
| E-A-004 | `Login_WrongPassword_ShowsError` | Wrong password | Error message on form |
| E-A-005 | `ProtectedRoute_RedirectsToLogin` | Navigate to `/tasks` without auth | Redirected to `/login` |

### 12.2 Task Lifecycle Tests (`TaskLifecycleTests.cs`)

| # | Test Name | Steps | Expected |
|---|-----------|-------|----------|
| E-T-001 | `CreateTask_ViaQuickAdd_AppearsInList` | Login → type in quick-add bar → press Enter | Success toast, task appears in Not Started group |
| E-T-002 | `CreateTask_ViaSlideOver_AllFieldsSaved` | Press N → fill all fields → Save | Task appears in list with all specified fields |
| E-T-003 | `EditTask_ChangesTitle_UpdatesInList` | Open task → click Edit → change title → Save | New title shown in task list |
| E-T-004 | `EditTask_ChangesStatus_MovesToCorrectGroup` | Edit task → change Status to InProgress → Save | Task moves to In Progress group |
| E-T-005 | `CompleteTask_ViaCheckbox_MovesToCompleted` | Click checkbox on task row | Task moves to Completed group, success toast |
| E-T-006 | `CompleteTask_ViaDetail_ShowsResultAnalysisPrompt` | Open task detail → Mark Complete | Result Analysis section becomes prominent with prompt |
| E-T-007 | `DeleteTask_ShowsUndoToast` | Delete a task | Task disappears, undo toast appears with countdown |
| E-T-008 | `UndoDelete_RestoresTask` | Delete → immediately click Undo in toast | Task reappears in list |
| E-T-009 | `SearchTask_FiltersByTitle` | Type in search box → debounce → results update | Only matching tasks shown |
| E-T-010 | `FilterTask_ByStatus_ShowsFiltered` | Click Filters → select Status: InProgress → apply | Only InProgress tasks shown, filter chip visible |
| E-T-011 | `ClearFilters_RestoresFullList` | Apply filter → click × on filter chip | All tasks shown again |
| E-T-012 | `BulkSelect_MarkComplete_UpdatesMultipleTasks` | Check 2 tasks → click Mark Complete in bulk toolbar | Both tasks move to Completed group, success toast |
| E-T-013 | `TaskDetail_ShowsActivityLog` | Create task → edit status → open detail | Activity log shows status change entry |
| E-T-014 | `InlineEdit_DoubleClickTitle_SavesOnEnter` | Double-click task title → type new name → Enter | Title updates in place |

### 12.3 Dashboard Tests (`DashboardTests.cs`)

| # | Test Name | Expected |
|---|-----------|----------|
| E-D-001 | `Dashboard_SummaryCards_ShowCorrectCounts` | Login → seed tasks → verify card counts match |
| E-D-002 | `Dashboard_OnboardingBanner_Visible_FirstLogin` | First login → welcome banner displayed |
| E-D-003 | `Dashboard_DismissBanner_Hides_OnReload` | Dismiss banner → reload → banner not shown |
| E-D-004 | `Dashboard_ChartsRender_NoErrors` | Login → dashboard loads → all 6 chart containers are visible, no error state |
| E-D-005 | `Dashboard_QuickAddBar_Visible_OnAllPages` | Navigate to Tasks → Audit → Settings → quick-add bar present on all pages |

### 12.4 Settings Tests (`SettingsTests.cs`)

| # | Test Name | Steps | Expected |
|---|-----------|-------|----------|
| E-S-001 | `GenerateApiKey_DisplaysKeyOnce` | Settings → API Keys → enter name → Generate | Key displayed in monospace with Copy button |
| E-S-002 | `GenerateApiKey_AfterNavAway_KeyNotShown` | Generate key → navigate away → return to API Keys | Full key no longer visible, only prefix shown in table |
| E-S-003 | `CopyApiKey_CopiedToClipboard` | Generate key → click Copy | Button shows "✓ Copied" for ~2s |
| E-S-004 | `DeactivateApiKey_AppearsInactive` | Deactivate key via Actions menu | Key status shows "Inactive" in table |
| E-S-005 | `ThemeToggle_Dark_ChangesUI` | Settings → Appearance → click Dark | Background color changes to dark theme |
| E-S-006 | `ThemeToggle_Dark_PersistsOnReload` | Select Dark → reload page | Dark theme still active after reload |
| E-S-007 | `ExportCsv_DownloadsFile` | Settings → Export → Export Tasks as CSV | File download triggered |

### 12.5 Keyboard Shortcuts Tests

| # | Test Name | Expected |
|---|-----------|----------|
| E-K-001 | `Shortcut_N_OpensCreatePanel` | Press N on task list → slide-over opens |
| E-K-002 | `Shortcut_ForwardSlash_FocusesSearch` | Press / → search input is focused |
| E-K-003 | `Shortcut_QuestionMark_OpensShortcutOverlay` | Press ? → keyboard shortcuts modal opens |
| E-K-004 | `Shortcut_Escape_ClosesSlideOver` | Open create panel → press Esc → panel closes |

### 12.6 Responsive Tests (`ResponsiveTests.cs`)

Run the full auth + create task + dashboard flow at three viewports:

| # | Test Name | Viewport | Steps | Expected |
|---|-----------|----------|-------|----------|
| E-R-001 | `Mobile_FullFlow` | 375×812 | Register → Create task → View dashboard | All pages render, bottom tab bar visible, no overflow |
| E-R-002 | `Tablet_FullFlow` | 768×1024 | Register → Create task → View dashboard | Collapsible sidebar present, layout correct |
| E-R-003 | `Desktop_FullFlow` | 1440×900 | Register → Create task → View dashboard | Persistent sidebar, dense layout |
| E-R-004 | `Mobile_BottomTabBar_Visible` | 375×812 | Any page | Bottom tab bar with 5 tabs visible |
| E-R-005 | `Desktop_Sidebar_Visible` | 1440×900 | Any page | Left sidebar with nav links visible |

---

## 13. Security Validation Tests

**Produces:** `SECURITY-VALIDATION.md`
**Run:** Phase 5, after core implementation complete

| # | Check | Method | Pass Criteria |
|---|-------|--------|---------------|
| S-001 | API keys stored hashed, not plaintext | Query DB directly after generating key | `KeyHash` != plaintext key value |
| S-002 | Full API key shown only once | Playwright: generate → navigate away → return | Full key not visible on return |
| S-003 | Revoked keys rejected immediately | Integration: deactivate key → use key | 401 response |
| S-004 | CORS not wildcard | Integration: inspect response headers | `Access-Control-Allow-Origin` is NOT `*` |
| S-005 | FluentValidation runs for all DTOs | Integration: send malformed JSON to each POST endpoint | 400 with `error.code = "VALIDATION_ERROR"` |
| S-006 | SQL injection prevention | Integration: send `' OR '1'='1` in search param | No injection; returns 0 or valid results |
| S-007 | Audit logs are read-only | Integration: attempt POST/PUT/DELETE on audit log endpoint | 404 or 405 |
| S-008 | Passwords hashed, not plaintext | Query DB after registration | `PasswordHash` is hashed, not plaintext |
| S-009 | No sensitive data in URL params | Review all endpoint specs | API keys, passwords never in query string |
| S-010 | Production errors have no stack traces | Integration with `ASPNETCORE_ENVIRONMENT=Production`: trigger 500 | Response body has no stack trace or type names |
| S-011 | All endpoints require auth | Integration: hit every endpoint without auth header | All return 401 |
| S-012 | All UI routes require login | Playwright: navigate to `/tasks`, `/audit`, `/settings` without auth | All redirect to `/login` |
| S-013 | Security headers present | Integration: inspect any response headers | `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy` present |
| S-014 | Rate limiting insertion point documented | Review `ARCHITECTURE.md §4.8` | Insertion point documented, NO rate limiting code present |

---

## Coverage Targets

| Layer | Target | How to measure |
|-------|--------|----------------|
| Service methods | >80% | `dotnet test --collect:"XPlat Code Coverage"` |
| Controller actions | >80% | Same |
| Repository methods | >70% | Same |
| Validators | 100% | Every rule tested |
| E2E flows | All 12 user flows covered | Manual map to E2E test cases |
