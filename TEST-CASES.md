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
4. [Unit Tests — Page Model Tests](#4-unit-tests--page-model-tests)
5. [Integration Tests — Authentication](#5-integration-tests--authentication)
6. [Integration Tests — Task Endpoints](#6-integration-tests--task-endpoints)
7. [Integration Tests — Tag Endpoints](#7-integration-tests--tag-endpoints)
8. [Integration Tests — API Key Endpoints](#8-integration-tests--api-key-endpoints)
9. [Integration Tests — Audit Endpoints](#9-integration-tests--audit-endpoints)
10. [Integration Tests — Stats Endpoints](#10-integration-tests--stats-endpoints)
11. [Integration Tests — Middleware & Pipeline](#11-integration-tests--middleware--pipeline)
12. [Playwright E2E Tests](#12-playwright-e2e-tests)
13. [Security Validation Tests](#13-security-validation-tests)
14. [Tags, Task Type, and Area Tests](#14-tags-task-type-and-area-tests)
15. [Integrations Page and Swagger Link](#15-integrations-page-and-swagger-link)
16. [Mobile Layout Tests](#16-mobile-layout-tests)
17. [Health & Diagnostics Tests](#17-health--diagnostics-tests)
18. [v1.12 — Tasks Page `show=` Filter (Active / Completed / All)](#19-v112--tasks-page-show-filter-active--completed--all)
19. [v1.13 — Clone Task (`POST /api/v1/tasks/{id}/clone`)](#20-v113--clone-task-post-apiv1tasksidclone)

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
| U-T-019 | `CreateTaskAsync_WritesCreatedActivityLog` | Valid request | `CreateTaskAsync` | `ActivityLogs` contains single entry: `FieldChanged="Created"`, `NewValue=title`, `ChangedBy=modifiedBy` |
| U-T-020 | `DeleteTaskAsync_WritesDeletedActivityLog` | Task exists | `DeleteTaskAsync` | `ActivityLogs` contains single entry: `FieldChanged="Deleted"`, `OldValue=title`, `ChangedBy=modifiedBy` |
| U-T-021 | `CompleteTaskAsync_LogsCorrectOldStatus` | Task with `Status=InProgress` | `CompleteTaskAsync` | Log entry: `OldValue="InProgress"`, `NewValue="Completed"` (not `Completed → Completed`) |
| U-T-022 | `PatchTaskAsync_WritesPerFieldActivityLogs` | Task with Title="Original", Priority=Low; patch Title+Priority | `PatchTaskAsync` | Two log entries: one for Title, one for Priority |
| U-T-023 | `PatchTaskAsync_UnchangedFields_DoNotProduceActivityLogs` | Task with Priority=High; patch with same Priority=High | `PatchTaskAsync` | `ActivityLogs` empty (no change = no log) |

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

### 1.4 ActivityLogService

| # | Test Name | Scenario | Expected |
|---|-----------|----------|----------|
| U-AL-001 | `GetPagedAsync_ReturnsMappedResponse` | Repo returns 1 log | `result.Data` has 1 entry with correct fields |
| U-AL-002 | `GetPagedAsync_CalculatesPageCount` | 3 total items, pageSize=2 | `TotalPages = 2` |
| U-AL-003 | `GetPagedAsync_EmptyResult_ReturnsEmptyData` | No logs | `result.Data` empty, `TotalCount = 0` |
| U-AL-004 | `GetForTaskAsync_DelegatesToRepository` | 2 logs for task | Returns 2 entries, repo called once |
| U-AL-005 | `GetForTaskAsync_EmptyForTaskWithNoHistory` | No history | Returns empty list |
| U-AL-006 | `GetPagedAsync_PassesQueryParamsToRepository` | Query with taskId + field filter | Repo receives same params |

### 1.5 StatsService

| # | Test Name | Scenario | Expected |
|---|-----------|----------|----------|
| U-ST-001 | `GetStatsAsync_CountsActiveTasks` | 5 NotStarted + 3 InProgress (not deleted) | `ActiveCount = 8` |
| U-ST-002 | `GetStatsAsync_ExcludesSoftDeletedFromCounts` | 3 tasks, 1 soft-deleted | Counts exclude soft-deleted |
| U-ST-003 | `GetCompletionsByWeekAsync_ReturnsLast12Weeks` | Tasks completed over 14 weeks | Returns array of 12 entries |
| U-ST-004 | `GetStatsAsync_CompletedTodayReflectsToday` | Task completed 3 hours ago, another completed yesterday | `CompletedToday = 1` |
| U-ST-005 | `GetStatsAsync_OverdueIsCorrectlyCalculated` | Task with `TargetDate = yesterday`, `Status = InProgress` | Counted as overdue |

### 1.6 ChangelogService

| # | Test Name | Scenario | Expected |
|---|-----------|----------|----------|
| U-CL-001 | `GetAll_ValidJson_ReturnsVersions` | Single version JSON | Returns 1 version |
| U-CL-002 | `GetLatest_ValidJson_ReturnsFirstEntry` | Single version `2.0` | Returns `2.0` |
| U-CL-003 | `GetAll_MultipleVersions_SortedDescending` | Versions 1.0, 1.2, 1.1 | Ordered 1.2 → 1.1 → 1.0 |
| U-CL-004 | `GetLatest_MultipleVersions_ReturnsHighestVersion` | Versions 1.0, 1.3, 1.1 | Latest = `1.3` |
| U-CL-005 | `GetAll_EmptyJson_ReturnsEmptyList` | `{}` | Empty list |
| U-CL-006 | `GetLatest_EmptyJson_ReturnsNull` | `{}` | `null` |
| U-CL-007 | `IsMajor_MajorVersionType_ReturnsTrue` | `versionType = "major"` | `IsMajor = true` |
| U-CL-008 | `IsMajor_MinorVersionType_ReturnsFalse` | `versionType = "minor"` | `IsMajor = false` |
| U-CL-009 | `GetAll_ParsesChanges_TypeAndDescription` | 2 changes in JSON | Both parsed with correct type/description |
| U-CL-010 | `GetAll_MalformedJson_ReturnsEmptyList` | Non-JSON string | Empty list (no exception) |

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

## 4. Unit Tests — Page Model Tests

**Project:** `TaskPilot.Tests.Unit/`
**Note:** Blazor WASM was replaced by Razor Pages in the MVC pivot. bUnit component tests are no longer applicable. Page model logic is covered by integration tests (Section 5–11) and E2E tests (Section 12). Unit tests focus on the service and repository layers where business logic lives.

_No dedicated page model unit tests are required — PageModel handlers are thin (validate → call service → set ViewData → return Page()/Redirect()); integration tests cover them end-to-end._

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

**Project:** `TaskPilot.Tests.Integration/Audit/AuditApiTests.cs`

| # | Test Name | Expected |
|---|-----------|----------|
| I-AU-001 | `GetAuditLogs_AuthenticatedUser_Returns200WithPagedResponse` | 200, `data` array + `meta` present |
| I-AU-002 | `GetAuditLogs_UnauthenticatedUser_Returns401` | 401 |
| I-AU-003 | `GetAuditLogs_ReturnsEmptyDataForNewUser` | 200, `data` length = 0 |
| I-AU-004 | `GetAuditLogs_WithPaginationParams_Returns200` | 200, `meta.page=1`, `meta.pageSize=5` |
| I-AU-005 | `GetAuditSummary_AuthenticatedUser_Returns200WithSummary` | 200, `data` has `totalRequests`, `getsToday`, `writesToday`, `activeApiKeys` |
| I-AU-006 | `GetAuditSummary_UnauthenticatedUser_Returns401` | 401 |
| I-AU-007 | `GetAuditSummary_NewUser_ReturnsZeroCounts` | `totalRequests=0`, `activeApiKeys=0` |

## 9b. Integration Tests — Activity Log Endpoints

**Project:** `TaskPilot.Tests.Integration/ActivityLogs/ActivityLogApiTests.cs`

| # | Test Name | Expected |
|---|-----------|----------|
| I-AL-001 | `GetActivityLogs_UnauthenticatedUser_Returns401` | 401 |
| I-AL-002 | `GetActivityLogs_NewUser_ReturnsEmptyList` | 200, `data` length = 0, `totalCount = 0` |
| I-AL-003 | `GetActivityLogs_AfterTaskUpdate_ContainsLog` | Create + update task → GET logs → at least 1 entry |
| I-AL-004 | `GetActivityLogs_FilterByTaskId_ReturnsOnlyThatTasksLogs` | 2 tasks updated → filter by taskId1 → all entries have `taskId = taskId1` |
| I-AL-005 | `GetActivityLogs_ReturnsPagedMetadata` | `meta.page=1`, `meta.pageSize=10` present |
| I-AL-006 | `GetActivityLogs_LogContainsExpectedFields` | Entry has `id`, `taskId`, `taskTitle`, `timestamp`, `fieldChanged`, `changedBy` |
| I-AL-007 | `GetActivityLogs_AfterTaskCreate_ContainsCreatedLog` | Create task → GET logs filtered by taskId → entry with `fieldChanged="Created"` present |
| I-AL-008 | `GetActivityLogs_AfterTaskDelete_DeletedLogStillVisible` | Create + delete task → GET logs filtered by taskId → `fieldChanged="Deleted"` entry visible despite soft-delete |

---

## 9c. Integration Tests — Changelog Page

**Project:** `TaskPilot.Tests.Integration/Changelog/ChangelogPageTests.cs`

| # | Test Name | Expected |
|---|-----------|----------|
| I-CL-001 | `ChangelogPage_UnauthenticatedUser_RedirectsToLogin` | 302 redirect to `/auth/login` |
| I-CL-002 | `ChangelogPage_AuthenticatedUser_Returns200` | 200 |
| I-CL-003 | `ChangelogPage_AuthenticatedUser_ShowsVersionEntries` | Content contains `v1.` and "What's new" |
| I-CL-004 | `ChangelogPage_AuthenticatedUser_ShowsChangeTypes` | Content contains "Feature", "Fix", or "Improvement" badge |

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
**Pattern:** xUnit test classes with shared `PlaywrightFixture` (ICollectionFixture).
**Collection:** `[Collection("Playwright")]` — tests share one browser instance and test server.
**Run command:** `dotnet test tests/TaskPilot.Tests.E2E/`
**Total:** 28 tests (23 original + 5 area/type/tag tests added in Phase 4).

### Setup
```bash
# Install Playwright browsers (run once after build):
pwsh tests/TaskPilot.Tests.E2E/bin/Debug/net10.0/playwright.ps1 install
```

### 12.1 Auth Tests (`Auth/AuthTests.cs`) — 5 tests

| # | Test Name | Steps | Expected |
|---|-----------|-------|----------|
| E-A-001 | `Register_NewUser_RedirectsToDashboard` | Navigate to `/auth/register` → fill email + password → submit | Redirects to `/`, dashboard loads |
| E-A-002 | `Login_ValidCredentials_RedirectsToDashboard` | Register → fresh login at `/auth/login` → submit | URL does not contain `/auth/login` |
| E-A-003 | `Login_WrongPassword_ShowsError` | `/auth/login` → wrong password → submit | Stays on `/auth/login`, error text visible |
| E-A-004 | `UnauthenticatedUser_RedirectedToLogin` | Navigate to `/` without auth | Redirected to URL containing `auth/login` |
| E-A-005 | `Logout_ClearsSession_RedirectsToLogin` | Authenticated page → click Logout button | Redirected to `/auth/login` |

### 12.2 Dashboard Tests (`Dashboard/DashboardTests.cs`) — 5 tests

| # | Test Name | Expected |
|---|-----------|----------|
| E-D-001 | `Dashboard_Loads_WithoutErrors` | Authenticated → wait for "Dashboard" text → no "An unhandled error" |
| E-D-002 | `Dashboard_ShowsFiveSummaryCards` | `.tp-stats-grid` loads → contains "Total Active", "Completed Today", "Overdue", "In Progress", "Blocked" |
| E-D-003 | `Dashboard_ChartsSection_Renders` | `.tp-charts-grid` loads → contains "Completed per Week" or "By Priority" |
| E-D-004 | `Dashboard_Navigation_TasksLinkWorks` | Click `a[href='/tasks']` → URL contains `/tasks` |
| E-D-005 | `Dashboard_Navigation_AuditLinkWorks` | Click `a[href='/audit']` → URL contains `/audit` |

### 12.3 Task Lifecycle Tests (`Tasks/TaskLifecycleTests.cs`) — 7 tests

| # | Test Name | Steps | Expected |
|---|-----------|-------|----------|
| E-T-001 | `Dashboard_AfterLogin_ShowsSummaryCards` | Authenticated → wait for `.tp-stats-grid` | Contains "Total Active" and "Overdue" |
| E-T-002 | `CreateTask_ViaNewTaskButton_AppearsInList` | `/tasks` → click "New Task" → fill title in modal → submit | Task title appears in page content |
| E-T-003 | `CreateTask_ViaQuickAdd_CreatesTask` | Dashboard → fill `input[name='title']` → submit → navigate to `/tasks` | Task title appears in task list |
| E-T-004 | `TaskList_SearchFilter_NarrowsResults` | `/tasks` → fill `#searchInput` with nonexistent text → wait 600ms | Content contains "No tasks" or "0 task" |
| E-T-005 | `TaskList_ToggleBoardView_ShowsKanbanColumns` | `/tasks` → click board view link → URL becomes `?view=board` | Content contains "Not Started" or "In Progress" |
| E-T-006 | `TaskDetail_NavigatingToTask_ShowsEditForm` | Create task → click task link → navigate to `/tasks/**` | Content contains "Priority" or "Status"; no unhandled error |
| E-T-007 | `DeleteTask_RemovesTaskFromList` | Create task → navigate to detail → accept confirm dialog → click Delete | Redirects to `/tasks`; no unhandled error |
| E-T-008 | `TaskDetail_AfterEdit_ShowsChangeHistory` | Create task → navigate to detail → change title → click "Save Changes" → reload | "Change History" section visible; no unhandled error |

### 12.4 Settings Tests (`Settings/SettingsTests.cs`) — 4 tests

| # | Test Name | Steps | Expected |
|---|-----------|-------|----------|
| E-S-001 | `Settings_PageLoads_ShowsApiKeySection` | `/settings` → wait for "API Keys" | Content contains "API Key" or "API Keys" |
| E-S-002 | `Settings_GenerateApiKey_ShowsKeyOnce` | `/settings` → fill `input[name='keyName']` → click "Generate Key" | Content contains "Copy", "created", or "tp_" |
| E-S-003 | `Settings_AppearanceSection_IsPresent` | `/settings` → wait for "Appearance" | Content contains "Appearance"; no unhandled error |
| E-S-004 | `Settings_ChangePassword_FormIsPresent` | `/settings` → wait for "Password" | `input[name='currentPassword']` is present |

### 12.5 Audit Tests (`Audit/AuditTests.cs`) — 6 tests

| # | Test Name | Expected |
|---|-----------|----------|
| E-AU-001 | `AuditPage_DefaultTab_ShowsTaskHistory` | `/audit` → `.nav-tabs` loads → content contains "Task History" and "API Access"; no error |
| E-AU-002 | `AuditPage_TaskHistoryTab_EmptyState_ShowsNoHistoryMessage` | `/audit?tab=tasks` → `.tp-card` loads → contains "No task history" or "Changes to tasks" |
| E-AU-003 | `AuditPage_ApiAccessTab_ShowsSummaryCards` | `/audit?tab=api` → `.tp-stats-grid` loads → contains "Total Requests" and "Active API Keys" |
| E-AU-004 | `AuditPage_ApiAccessTab_EmptyState_ShowsNoLogsMessage` | `/audit?tab=api` → `.tp-card` loads → contains "No audit" or "API key activity" |
| E-AU-005 | `AuditPage_TabSwitch_NavigatesToApiTab` | Click "API Access" tab → `.tp-stats-grid` loads → contains "Total Requests" |
| E-AU-006 | `AuditPage_TaskHistoryTab_AfterTaskEdit_ShowsLog` | Create task → edit title → `/audit?tab=tasks` → `.tp-card` loads | History entries visible; no unhandled error |

### 12.6 Changelog Tests (`Changelog/ChangelogTests.cs`) — 7 tests

| # | Test Name | Steps | Expected |
|---|-----------|-------|----------|
| E-CL-001 | `ChangelogPage_NavLink_IsVisibleInSidebar` | Authenticated → wait for `.tp-changelog-nav-link` | Contains "What's new"; no unhandled error |
| E-CL-002 | `ChangelogPage_NavLink_ShowsRenderedVersionNumber` | Wait for `.tp-version-pill` | Pill text matches `v\d+\.\d+` — catches unrendered Razor like `v@...` |
| E-CL-003 | `ChangelogPage_VersionBadges_ShowRenderedVersionNumbers` | `/changelog` → all `.tp-version-badge` elements | Each badge text matches `v\d+\.\d+` |
| E-CL-004 | `ChangelogPage_NavigatingToPage_ShowsVersionHistory` | `/changelog` → `.tp-changelog` loads | Matches `v\d+\.\d+`; does not contain `@version`; no unhandled error |
| E-CL-005 | `ChangelogPage_ShowsMajorVersionBadge` | `/changelog` → `.tp-changelog-version` loads | Contains "Major" badge |
| E-CL-006 | `ChangelogPage_ShowsChangeTypeBadges` | `/changelog` → `.tp-change-badge` elements present | At least one badge found |
| E-CL-007 | `ChangelogPage_NavLink_ClickNavigatesToPage` | Click `.tp-changelog-nav-link` → wait for `/changelog` URL | Contains "What's new"; no unhandled error |

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

## 14. Tags, Task Type, and Area Tests

> Tests added in Phase 4 to cover the Area (Personal/Work) field, TaskType lookup, and Tags UI features.

### 14.1 Unit Tests — TaskService (Area & TaskType)

**File:** `TaskPilot.Tests.Unit/Services/TaskServiceTests.cs`

| # | Test Name | Type | Description | Expected |
|---|-----------|------|-------------|----------|
| U-T-024 | `CreateTaskAsync_WithArea_PersistsAreaOnTask` | Unit | Create task with `Area = Work` | Returned and persisted task has `Area = Work` |
| U-T-025 | `CreateTaskAsync_WithTaskTypeId_PersistsTaskTypeId` | Unit | Create task with a valid `TaskTypeId` | Returned task has matching `TaskTypeId` |
| U-T-026 | `CreateTaskAsync_DefaultArea_IsPersonal` | Unit | Create task without specifying `Area` | `Area = Personal` (default enum value 0) |
| U-T-027 | `UpdateTaskAsync_ChangesArea_FromPersonalToWork` | Unit | Create task with `Area=Personal`, update to `Area=Work` | Updated task has `Area = Work` |
| U-T-028 | `PatchTaskAsync_WithArea_UpdatesArea` | Unit | Patch task with `Area = Work` | Task `Area` changes to `Work` |
| U-T-029 | `PatchTaskAsync_WithTaskTypeId_UpdatesTaskTypeId` | Unit | Patch task with a new `TaskTypeId` | Task `TaskTypeId` is updated |
| U-T-030 | `PatchTaskAsync_NullArea_DoesNotChangeArea` | Unit | Patch task with `Area = null` in patch doc | Task `Area` remains unchanged |

### 14.2 Unit Tests — TaskTypeService

**File:** `TaskPilot.Tests.Unit/Services/TaskTypeServiceTests.cs`

| # | Test Name | Type | Description | Expected |
|---|-----------|------|-------------|----------|
| U-TT-001 | `GetAllActiveAsync_ReturnsAllActiveTypes_OrderedBySortOrder` | Unit | Repo returns 3 task types with varying `SortOrder`; 1 inactive | Returns 2 active types ordered by `SortOrder` ascending |
| U-TT-002 | `GetAllActiveAsync_EmptyRepository_ReturnsEmptyList` | Unit | Repo returns empty list | Returns empty list, no exception |

### 14.3 Unit Tests — StatsService (Area & Tags)

**File:** `TaskPilot.Tests.Unit/Services/StatsServiceTests.cs`

| # | Test Name | Type | Description | Expected |
|---|-----------|------|-------------|----------|
| U-ST-006 | `GetTaskStatsAsync_CompletionsByArea_CountsPersonalAndWorkSeparately` | Unit | 3 completed Personal + 2 completed Work + 1 not-started Personal | `CompletionsByArea.Personal = 3`, `CompletionsByArea.Work = 2` |
| U-ST-007 | `GetTaskStatsAsync_TopTags_ReturnsTopFiveByTaskCount` | Unit | 6 tags with task counts 6, 5, 4, 3, 2, 1 respectively | Returns exactly 5 entries ordered descending; tag with count 1 excluded; top tag is "alpha" with count 6 |

### 14.4 Integration Tests — Task Type Endpoints

**File:** `TaskPilot.Tests.Integration/TaskTypes/TaskTypeApiTests.cs`

| # | Test Name | Type | Description | Expected |
|---|-----------|------|-------------|----------|
| I-TT-001 | `GetTaskTypes_Unauthenticated_Returns401` | Integration | GET `/api/v1/task-types` without auth | 401 Unauthorized |
| I-TT-002 | `GetTaskTypes_Authenticated_ReturnsSeededList` | Integration | Seed 6 standard types; GET `/api/v1/task-types` | 200; `data` array length ≥ 6; all 6 names present (Task, Goal, Habit, Meeting, Note, Event); items ordered by `sortOrder` ascending |
| I-TT-003 | `GetTaskTypes_Authenticated_AllTypesHaveNameAndId` | Integration | Seed 6 types; GET `/api/v1/task-types` | Every item in `data` has `id > 0` and non-empty `name` |

### 14.5 Integration Tests — Task Endpoints (Area & Tags)

**File:** `TaskPilot.Tests.Integration/Tasks/TasksApiTests.cs`

| # | Test Name | Type | Description | Expected |
|---|-----------|------|-------------|----------|
| I-T-036 | `CreateTask_WithAreaWork_PersistsAndReturnsWork` | Integration | POST task with `area = 1` (Work) | 201; response `data.area = 1`; DB record has `Area = Work` |
| I-T-037 | `CreateTask_WithTaskTypeId_ReturnsTaskTypeName` | Integration | Seed a TaskType; POST task with that `taskTypeId` | 201 or GET confirms `taskTypeName` matches seeded type name |
| I-T-038 | `CreateTask_WithTagIds_ReturnsTagsInResponse` | Integration | Create a tag; POST task with `tagIds = [tagId]` | 201; GET task confirms `tags` array includes the tag name |
| I-T-039 | `GetTasks_FilterByArea_ReturnsOnlyMatchingTasks` | Integration | Create 1 Personal + 1 Work task; GET `?area=1` | Only the Work task appears in `data` |
| I-T-040 | `GetTasks_FilterByTaskTypeId_ReturnsOnlyMatchingTasks` | Integration | Seed TaskType; create 1 task with type + 1 without; GET `?taskTypeId=<id>` | Only typed task appears |
| I-T-041 | `GetTasks_FilterByTagIds_AndLogic_ReturnsOnlyTasksWithAllTags` | Integration | Create 2 tags; task A has both, task B has one, task C has neither; GET `?tagIds=id1,id2` | Only task A returned |

### 14.6 E2E Tests — Area, Task Type, and Tags UI

**File:** `TaskPilot.Tests.E2E/Tasks/TaskAreaTypeTagTests.cs`

| # | Test Name | Type | Steps | Expected |
|---|-----------|------|-------|----------|
| E-ATT-001 | `TaskList_AreaFilter_Work_ShowsOnlyWorkTasks` | E2E | Create 1 Work + 1 Personal task via modal; click Work area filter | Page does not show "An unhandled error"; filter applies without crash |
| E-ATT-002 | `TaskList_AreaFilter_All_ShowsBothAreas` | E2E | Navigate to `/tasks`; click "All" filter if present | Page loads without "An unhandled error" |
| E-ATT-003 | `TaskCreateForm_AreaToggle_DefaultIsPersonal` | E2E | Open "New Task" modal; inspect area control | If area control exists, Personal is selected by default; no unhandled error |
| E-ATT-004 | `TaskCreateForm_SelectType_AppearsOnCard` | E2E | Open modal; select "Meeting" from task type dropdown if present; submit | Task title appears in page content; no unhandled error |
| E-ATT-005 | `TaskCreateForm_AddTag_AppearsAsTagPill` | E2E | Open modal; type "urgent" in tag input if present; submit | Task title appears in page content; no unhandled error |

---

## 15. Integrations Page and Swagger Link

### 15.1 E2E Tests — Integrations Page

**File:** `TaskPilot.Tests.E2E/Integrations/IntegrationsPageTests.cs`

| # | Test Name | Type | Steps | Expected |
|---|-----------|------|-------|----------|
| E-INT-001 | `IntegrationsPage_AuthenticatedUser_LoadsWithoutError` | E2E | Authenticate; navigate to `/integrations` | Page loads; title contains "Integrations"; no unhandled error |
| E-INT-002 | `IntegrationsPage_UnauthenticatedUser_RedirectsToLogin` | E2E | No auth; navigate to `/integrations` | Redirected to `/auth/login` |
| E-INT-003 | `IntegrationsPage_ContainsApiKeySection` | E2E | Authenticate; navigate to `/integrations` | Page contains "API key", "X-Api-Key", or "Quick Start" |
| E-INT-004 | `IntegrationsPage_ContainsCurlExample` | E2E | Authenticate; navigate to `/integrations` | Page contains "curl" |
| E-INT-005 | `IntegrationsPage_ContainsClaudeToolDefinition` | E2E | Authenticate; navigate to `/integrations` | Page contains "input_schema" or "Claude" |
| E-INT-006 | `IntegrationsPage_ContainsOpenAiToolDefinition` | E2E | Authenticate; navigate to `/integrations` | Page contains "OpenAI", "GPT", or `"type": "function"` |
| E-INT-007 | `IntegrationsPage_ContainsMcpComingSoon` | E2E | Authenticate; navigate to `/integrations` | Page contains "Coming Soon", "coming soon", or "MCP" |
| E-INT-008 | `IntegrationsPage_SwaggerLink_IsPresent` | E2E | Authenticate; navigate to `/integrations` | Page references "/swagger" or "swagger" |
| E-INT-009 | `IntegrationsPage_CopyButton_IsClickableWithoutError` | E2E | Authenticate; navigate to `/integrations`; click first copy button | No unhandled error |
| E-INT-010 | `NavSidebar_ContainsIntegrationsLink` | E2E | Authenticate; navigate to `/`; inspect sidebar | `a[href='/integrations']` exists |
| E-INT-011 | `SettingsPage_ContainsApiReferenceSection` | E2E | Authenticate; navigate to `/settings` | Page contains "API Reference" or "Integrations" |

---

### 16. Mobile Layout Tests

**File:** `TaskPilot.Tests.E2E/Mobile/MobileLayoutTests.cs`
**Viewports:** Mobile = 390×844 (iPhone 12 Pro), Tablet = 768×1024 (iPad)

| # | Test Name | Type | Steps | Expected |
|---|-----------|------|-------|----------|
| MOB-001 | `MobileViewport_ShowsMobileHeader` | E2E | Authenticate at 390px | `.tp-mobile-header` is visible |
| MOB-002 | `MobileViewport_HamburgerButtonVisible` | E2E | Authenticate at 390px | `.tp-hamburger` button is visible |
| MOB-003 | `MobileViewport_SidebarHiddenByDefault` | E2E | Authenticate at 390px | Sidebar bounding box is off-screen (right edge ≤ 0) |
| MOB-004 | `MobileViewport_HamburgerOpensSidebar` | E2E | Click hamburger; wait 400ms | `.tp-sidebar` has class `open` |
| MOB-005 | `MobileViewport_BackdropVisibleWhenSidebarOpen` | E2E | Click hamburger; wait 400ms | `.tp-sidebar-backdrop` has class `active` |
| MOB-006 | `MobileViewport_BackdropClickClosesSidebar` | E2E | Click hamburger; click backdrop area (x=310,y=400); wait 400ms | `.tp-sidebar` does NOT have class `open` |
| MOB-007 | `MobileViewport_CanNavigateViaSidebar` | E2E | Click hamburger; click Tasks nav link | Browser navigates to `/tasks` |
| MOB-008 | `TabletViewport_SidebarIconRailVisible` | E2E | Authenticate at 768px | Sidebar visible; `.tp-brand-text` hidden (icon-only rail) |
| MOB-009 | `TabletViewport_NoHamburgerButton` | E2E | Authenticate at 768px | `.tp-mobile-header` is hidden |
| MOB-010 | `MobileViewport_ChangelogShowsV17` | E2E | Authenticate at 390px; open sidebar; click What's new | Changelog page contains "1.7" |

---

## 17. Health & Diagnostics Tests

Test plan for the Health subsystem (ARCHITECTURE.md §12). All test IDs prefixed `HLTH-`.

### 17.1 Unit Tests — BuildInfo & Health Components

**File:** `tests/TaskPilot.Tests.Unit/Diagnostics/BuildInfoTests.cs`

| # | Test Name | Type | Steps | Expected |
|---|-----------|------|-------|----------|
| HLTH-001 | `BuildInfo_Version_ReadsFromAssembly` | Unit | Read `BuildInfo.Version` | Returns non-empty SemVer string matching `<Version>` in csproj |
| HLTH-002 | `BuildInfo_GitCommit_ReadsFromAssemblyMetadata` | Unit | Read `BuildInfo.GitCommit` | Returns 40-char hex string OR `"unknown"` (never null/empty) |
| HLTH-003 | `BuildInfo_GitCommitShort_IsSevenChars` | Unit | Read `BuildInfo.GitCommitShort` | Returns 7-char string OR `"unknown"` |
| HLTH-004 | `BuildInfo_BuildTimestampUtc_IsParseable` | Unit | Read `BuildInfo.BuildTimestampUtc` | Returns DateTime in UTC, within last 365 days |
| HLTH-005 | `BuildInfo_FallsBackGracefully_WhenMetadataMissing` | Unit | Reflection-mock missing AssemblyMetadata | Returns `"unknown"` instead of throwing |

**File:** `tests/TaskPilot.Tests.Unit/Diagnostics/HealthCheckTests.cs`

| # | Test Name | Type | Steps | Expected |
|---|-----------|------|-------|----------|
| HLTH-010 | `DatabaseCheck_Healthy_WhenCanConnect` | Unit | Mock `DbContext.Database.CanConnectAsync()` → true | Status = healthy, Duration > 0 |
| HLTH-011 | `DatabaseCheck_Unhealthy_WhenConnectionFails` | Unit | Mock `CanConnectAsync` throws | Status = unhealthy, Message contains exception type |
| HLTH-012 | `DatabaseCheck_Unhealthy_WhenTimesOut` | Unit | Mock `CanConnectAsync` delays > 2s | Status = unhealthy, Message contains "timeout" |
| HLTH-013 | `MigrationsCheck_Healthy_WhenNoPending` | Unit | Mock `GetPendingMigrationsAsync` → empty | Status = healthy, Data["pendingMigrations"] = "0" |
| HLTH-014 | `MigrationsCheck_Unhealthy_WhenPendingExist` | Unit | Mock returns 2 pending | Status = unhealthy, Data["pendingMigrations"] = "2" |
| HLTH-015 | `ConfigCheck_Unhealthy_WhenRequiredKeyMissing` | Unit | IConfiguration without `ApiKey:HmacSigningKey` | Status = unhealthy, Message names missing key |
| HLTH-016 | `AuthHandlersCheck_Healthy_WhenBothSchemesRegistered` | Unit | Mock `IAuthenticationSchemeProvider` with Identity + ApiKeyScheme | Status = healthy |
| HLTH-017 | `AuthHandlersCheck_Unhealthy_WhenApiKeySchemeMissing` | Unit | Provider lacks ApiKeyScheme | Status = unhealthy |
| HLTH-018 | `McpCheck_Healthy_WhenEndpointRegistered` | Unit | Mock `EndpointDataSource` containing `/mcp` | Status = healthy, IsRequired = false |
| HLTH-019 | `McpCheck_Unhealthy_WhenEndpointMissing` | Unit | EndpointDataSource without `/mcp` | Status = unhealthy, IsRequired = false (degraded only) |
| HLTH-020 | `TempWritableCheck_Healthy_WhenCanWriteAndDelete` | Unit | Real Path.GetTempPath() | Status = healthy, leaves no file behind |
| HLTH-021 | `AssemblyMetadataCheck_Unhealthy_WhenCommitUnknown` | Unit | BuildInfo.GitCommit = "unknown" | Status = unhealthy, IsRequired = false |
| HLTH-022 | `HealthService_Aggregates_HealthyWhenAllHealthy` | Unit | All checks healthy | Overall status = healthy, HTTP would be 200 |
| HLTH-023 | `HealthService_Aggregates_503WhenRequiredFails` | Unit | One required check unhealthy, optional healthy | Overall status = unhealthy, HTTP = 503 |
| HLTH-024 | `HealthService_Aggregates_DegradedWhenOnlyOptionalFails` | Unit | All required healthy, one optional unhealthy | Overall status = degraded, HTTP = 200 |

### 17.2 Integration Tests — Health Endpoints

**File:** `tests/TaskPilot.Tests.Integration/Health/HealthEndpointTests.cs` (uses WebApplicationFactory)

| # | Test Name | Type | Steps | Expected |
|---|-----------|------|-------|----------|
| HLTH-030 | `Version_Returns200_AndEnvelope` | Integration | GET `/api/v1/health/version` (no auth) | 200, body matches `ApiResponse<VersionResponse>`, `meta.requestId` is a GUID |
| HLTH-031 | `Version_BodyMatchesBuildInfo` | Integration | Compare response to `BuildInfo` static | Version, GitCommit, GitCommitShort all equal |
| HLTH-032 | `Version_HasNoCacheHeaders` | Integration | Inspect response headers | `Cache-Control` contains `no-store`, `Pragma: no-cache`, `Expires: 0` |
| HLTH-033 | `Version_HasCustomVersionHeaders` | Integration | Inspect response headers | `X-TaskPilot-Version` and `X-TaskPilot-Commit` present and equal to body |
| HLTH-034 | `Live_Returns200_Always` | Integration | GET `/api/v1/health/live` | 200, `data.status = "alive"` |
| HLTH-035 | `Ready_Returns200_WhenAllRequiredHealthy` | Integration | GET `/api/v1/health/ready` against in-memory healthy DB | 200, status = healthy |
| HLTH-036 | `Ready_Returns503_WhenDatabaseDown` | Integration | Replace DbContext with one whose connection string is invalid | 503, envelope still well-formed, status = unhealthy |
| HLTH-037 | `Full_Returns200_AndIncludesAllChecks` | Integration | GET `/api/v1/health/full` | 200, `data.checks` contains all 7 named checks |
| HLTH-038 | `Full_PerCheckHasDuration` | Integration | GET `/api/v1/health/full` | Every check has `duration > 0` |
| HLTH-039 | `Full_Returns200WhenOnlyOptionalDegraded` | Integration | Disable MCP registration | 200, overall = degraded, mcp check = unhealthy |
| HLTH-040 | `Full_Returns503_WhenMigrationsPending` | Integration | Spin up DB without applying migrations | 503, migrations check = unhealthy |
| HLTH-041 | `Assets_Returns200_WithManifest` | Integration | GET `/api/v1/health/assets` | 200, `data.assets` non-empty, every value matches `^sha256-` |
| HLTH-042 | `Assets_HashStable_OnRepeatedCalls` | Integration | Call twice | Identical hash dictionary |
| HLTH-043 | `HealthEndpoints_NotInAuditLog` | Integration | Hit each endpoint, then GET `/api/v1/audit` | Zero entries with path starting `/api/v1/health` |
| HLTH-044 | `HealthEndpoints_AnonymousAccess` | Integration | Hit each without cookie or API key | All return non-401 |
| HLTH-045 | `Ready_RespondsWithin500ms` | Integration | Time the call against healthy DB | Duration < 500ms |

### 17.3 E2E Tests — Public Health Page

**File:** `tests/TaskPilot.Tests.E2E/Health/HealthPageTests.cs`

| # | Test Name | Type | Steps | Expected |
|---|-----------|------|-------|----------|
| HLTH-050 | `HealthPage_LoadsAnonymously` | E2E | Navigate to `/health` without login | Page renders, no redirect to login |
| HLTH-051 | `HealthPage_ShowsHealthyBadge` | E2E | Navigate to `/health` against healthy app | `.tp-health-status` text = "HEALTHY", green |
| HLTH-052 | `HealthPage_VersionMatchesApi` | E2E | Read version pill text on page; GET `/api/v1/health/version` | Page version equals `data.version` |
| HLTH-053 | `HealthPage_CommitMatchesApi` | E2E | Read commit on page; GET `/api/v1/health/version` | Page short commit equals `data.gitCommitShort` |
| HLTH-054 | `HealthPage_PerCheckRowsRendered` | E2E | Count rows in checks table | At least 7 rows, each with name + status + duration |
| HLTH-055 | `HealthPage_RawJsonLink_GoesToFullEndpoint` | E2E | Click "Raw JSON" link | Browser navigates to `/api/v1/health/full` |
| HLTH-056 | `SidebarVersionPill_LinksToHealthPage` | E2E | Authenticate, click version pill in sidebar | Browser navigates to `/health` |
| HLTH-057 | `SidebarVersionPill_TextMatchesApiVersion` | E2E | Read pill text; GET `/api/v1/health/version` | Pill text contains `data.version` and `data.gitCommitShort` |

### 17.4 Deployment Smoke Test

**File:** `scripts/smoke.ps1` and mirrored xUnit `tests/TaskPilot.Tests.Integration/Smoke/DeploymentSmokeTests.cs` (parameterized by env var `SMOKE_BASE_URL`, default `http://localhost:5125`)

| # | Test Name | Type | Steps | Expected |
|---|-----------|------|-------|----------|
| HLTH-060 | `Smoke_VersionEndpointReachable` | Smoke | GET `{BaseUrl}/api/v1/health/version` | 200, parseable VersionResponse |
| HLTH-061 | `Smoke_DeployedCommitMatchesExpected` | Smoke | Compare `data.gitCommitShort` to env `EXPECTED_COMMIT` | Equal (case-insensitive) |
| HLTH-062 | `Smoke_FullHealthGreen` | Smoke | GET `{BaseUrl}/api/v1/health/full` | 200, status = healthy |
| HLTH-063 | `Smoke_NoCdnCachingDetected` | Smoke | GET `/api/v1/health/version` twice with cache-buster query | Both return same Version+Commit; no `Age` header on response |
| HLTH-064 | `Smoke_AssetManifestMatchesServedAssets` | Smoke | For each asset in manifest, GET it and SHA256 the body | Every hash equals manifest entry |
| HLTH-065 | `Smoke_RunsAgainstLocalAndAzure` | Smoke | Run twice with `SMOKE_BASE_URL=http://localhost:5125` then `=https://taskpilot.azurewebsites.net` | Both pass |

---

## 18. Incomplete View, Overdue Filter, and Tag Editing

### 18.1 Unit Tests — TagService (`Services/TagServiceTests.cs`)

| # | Test Name | Scenario | Expected |
|---|-----------|----------|----------|
| U-TAG-005 | `GetAllTagsAsync_PopulatesTaskCountFromRepository` | Repo returns `(Tag, count)` tuples | `TaskCount` flows into `TagResponse` |
| U-TAG-006 | `UpdateTagAsync_ValidRequest_UpdatesNameAndColor` | Tag exists, no name conflict | Returns updated `TagResponse`; entity name + color + LastModifiedBy mutated |
| U-TAG-007 | `UpdateTagAsync_TagDoesNotExist_ReturnsNull` | Repo returns null | Returns null; SaveChanges not called |
| U-TAG-008 | `UpdateTagAsync_WrongUser_ReturnsNull` | Tag belongs to a different user | Returns null; original entity untouched |
| U-TAG-009 | `UpdateTagAsync_DuplicateNameForSameUser_ThrowsInvalidOperationException` | Another tag with target name exists for same user | Throws; SaveChanges not called |
| U-TAG-010 | `UpdateTagAsync_SameNameDifferentUser_DoesNotConflict` | Other user owns the same name | Succeeds (scope is per-user) |
| U-TAG-011 | `UpdateTagAsync_OnlyColorChanged_AllowsSameName` | Renaming to the existing same name | Succeeds without invoking the duplicate check |
| U-TAG-012 | `UpdateTagAsync_SetsLastModifiedByToCaller` | Valid update | `LastModifiedBy` reflects the calling identity |

### 18.2 Unit Tests — Validator (`Validators/UpdateTagRequestValidatorTests.cs`)

| # | Test Name | Scenario | Expected |
|---|-----------|----------|----------|
| U-V-UTAG-001 | `Validate_ValidRequest_IsValid` | Name + valid 6-digit hex | Valid |
| U-V-UTAG-002 | `Validate_EmptyName_HasError` | Empty name | Invalid; error on `Name` |
| U-V-UTAG-003 | `Validate_NameOver50Chars_HasError` | 51-char name | Invalid; error on `Name` |
| U-V-UTAG-004 | `Validate_InvalidHexColor_HasError` | Color is not a hex value | Invalid; error on `Color` |
| U-V-UTAG-005 | `Validate_ValidHexColor_IsValid` | Lower- and upper-case hex | Both valid |

### 18.3 Unit Tests — StatsService (`Services/StatsServiceTests.cs`)

| # | Test Name | Scenario | Expected |
|---|-----------|----------|----------|
| U-S-IBS-001 | `GetTaskStatsAsync_IncompleteByStatus_CountsCorrectPerStatus` | Mixed statuses | NotStarted/InProgress/Blocked counts + Total |
| U-S-IBS-002 | `GetTaskStatsAsync_IncompleteByStatus_TotalEqualsTotalActive` | Mixed statuses | `Total === TotalActive` |
| U-S-IBS-003 | `GetTaskStatsAsync_IncompleteByStatus_NoIncompleteTasks_ReturnsZeroes` | Only Completed/Cancelled | All four fields are 0 |
| U-S-IBS-004 | `GetTaskStatsAsync_IncompleteByStatus_ScopedToUser` | Two users | Only caller's tasks counted |

### 18.4 Unit Tests — TaskRepository (`Services/TaskRepositoryFilterTests.cs`)

| # | Test Name | Scenario | Expected |
|---|-----------|----------|----------|
| U-TR-001 | `GetPagedAsync_WithIncompleteFilter_ReturnsOnlyNotStartedInProgressBlocked` | Mixed statuses, IncludeOnlyIncomplete=true | 3 rows with statuses 0/1/2 |
| U-TR-002 | `GetPagedAsync_WithIncompleteFilter_ExcludesCompleted` | One Completed task | None returned |
| U-TR-003 | `GetPagedAsync_WithIncompleteFilter_ExcludesCancelled` | One Cancelled task | None returned |
| U-TR-004 | `GetPagedAsync_WithIncompleteFilter_ExcludesSoftDeleted` | One IsDeleted=true | Total = 1 (the non-deleted) |
| U-TR-005 | `GetPagedAsync_WithIncompleteFilter_WrongUser_ReturnsEmpty` | Tasks owned by another user | Empty result |
| U-TR-006 | `GetPagedAsync_WithOverdueOnly_FiltersToTargetDateInPastAndNotNull` | yesterday/tomorrow/null mix | Only yesterday returned |
| U-TR-007 | `GetPagedAsync_WithIncompleteAndOverdue_Composes` | Mix incl. completed-overdue + blocked-no-date | Only "incomplete AND overdue" returned |
| U-TR-008 | `GetPagedAsync_WithIncompleteFilter_DefaultSort_PriorityDescThenTargetDateAscNullsLast` | High/null + High/+1 + High/+7 + Low/now | High items first, dated rows before nulls within priority, Low last |
| U-TR-009 | `GetPagedAsync_NoFilters_DoesNotApplyIncompleteOrOverdue` | Default ctor, mixed statuses | All rows returned (defaults preserved) |

### 18.5 Integration Tests — Tag Endpoints (`Tags/TagsApiTests.cs`)

| # | Test Name | Scenario | Expected |
|---|-----------|----------|----------|
| I-TAG-005 | `UpdateTag_ValidRequest_Returns200WithUpdatedTag` | PUT new name + color | 200; envelope has updated fields |
| I-TAG-006 | `UpdateTag_NonExistentTag_Returns404` | PUT a random GUID | 404 |
| I-TAG-007 | `UpdateTag_OtherUsersTag_Returns404` | User2 PUTs User1's tag | 404 (no cross-user leakage) |
| I-TAG-008 | `UpdateTag_DuplicateNameForSameUser_Returns409` | Two tags exist; rename B → A | 409 |
| I-TAG-009 | `UpdateTag_InvalidPayload_Returns400` | Empty name + bad color | 400 |
| I-TAG-010 | `UpdateTag_Unauthenticated_Returns401` | No auth | 401/redirect |
| I-TAG-011 | `GetTags_TagAssignedToTask_PopulatesTaskCount` | Tag attached to one task | `taskCount = 1` in list response |

### 18.6 Integration Tests — Task Endpoints (`Tasks/TasksApiTests.cs`)

| # | Test Name | Scenario | Expected |
|---|-----------|----------|----------|
| I-T-IV-001 | `GetTasks_WithIncludeOnlyIncomplete_ReturnsOnlyIncompleteStatuses` | 5 tasks across all 5 statuses | 3 returned (NotStarted/InProgress/Blocked) |
| I-T-IV-002 | `GetTasks_WithOverdueOnly_ReturnsOnlyOverdueWithDate` | overdue / future / no-date | Only the overdue task |
| I-T-IV-003 | `GetTasks_IncompleteAndOverdue_Composes` | Mixed incl. completed-overdue | Only "incomplete AND overdue" |
| I-T-IV-004 | `GetStats_IncludesIncompleteByStatus` | Mixed statuses | Stats envelope includes correct `incompleteByStatus.{notStarted,inProgress,blocked,total}` |

### 18.7 E2E Tests — Playwright (Dashboard / Tasks / Settings)

| # | Test Name | File | Notes |
|---|-----------|------|------|
| E2E-IV-001 | `Dashboard_IncompleteCard_NavigatesToFilteredTasksView` | `Dashboard/DashboardTests.cs` | Quick-add a task, click NotStarted sub-tile, assert URL contains `?show=active&status=NotStarted` |
| E2E-IV-002 | `Dashboard_OverdueCard_NavigatesToOverdueActiveView` | `Dashboard/DashboardTests.cs` | Click `.tp-stat-card-link` → assert URL contains `?show=active&overdue=true` |
| E2E-IV-003 | `TasksPage_DefaultLoad_ShowsActiveTasksOnly` | `Tasks/TaskLifecycleTests.cs` | Cold load `/tasks` (no query string); assert Completed/Cancelled tasks not present; assert Active segment has `aria-pressed="true"` |
| E2E-IV-004 | `TasksPage_ShowSegment_Completed_ShowsOnlyTerminalStatuses` | `Tasks/TaskLifecycleTests.cs` | Navigate to `?show=completed`; assert only Completed/Cancelled rows visible; assert Completed segment selected |
| E2E-IV-005 | `TasksPage_ShowSegment_All_ShowsAllStatuses` | `Tasks/TaskLifecycleTests.cs` | Navigate to `?show=all`; assert tasks of every status are present |
| E2E-IV-006 | `TasksPage_BoardView_ActiveShow_OnlyOpenColumns` | `Tasks/TaskLifecycleTests.cs` | Open board view with default show=active; assert only Not Started / In Progress / Blocked columns rendered |
| E2E-IV-007 | `TasksPage_BoardView_CompletedShow_OnlyTerminalColumns` | `Tasks/TaskLifecycleTests.cs` | Open `?view=board&show=completed`; assert Completed and Cancelled columns present, open columns absent |
| E2E-IV-008 | `TasksPage_OverdueChip_TogglesAndUpdatesUrl` | `Tasks/TaskLifecycleTests.cs` | Click Overdue chip, assert `aria-pressed="true"` and URL `?overdue=true`; click again, assert removed |
| E2E-IV-009 | `TasksPage_FilterPersistence_SidebarAwayAndBack` | `Tasks/TaskLifecycleTests.cs` | Apply show=all + area=Work; navigate to Dashboard via sidebar; navigate back via sidebar; assert both filters restored (sidebar link href rewrite path) |
| E2E-IV-010 | `TasksPage_FilterPersistence_AddressBarRehydrate` | `Tasks/TaskLifecycleTests.cs` | Apply filters; navigate to bare `/tasks` in address bar same tab; assert `location.replace` fires and filters are restored |
| E2E-IV-011 | `TasksPage_FilterPersistence_BackButton_NoLoop` | `Tasks/TaskLifecycleTests.cs` | Navigate from filtered Tasks to Dashboard; press back; assert lands on previous page (Dashboard), not in a redirect loop |
| E2E-IV-012 | `TasksPage_ResetFilters_ClearsSessionStorageAndNavigates` | `Tasks/TaskLifecycleTests.cs` | Apply filters; click Reset filters link; assert sessionStorage key removed; assert URL is `/tasks?show=active` |
| E2E-IV-013 | `TasksPage_NewSession_StartsWithActiveDefault` | `Tasks/TaskLifecycleTests.cs` | Close browser context (new Playwright context = clean sessionStorage); navigate to `/tasks`; assert Active segment selected, no rehydration |
| E2E-TAG-001 | `Settings_EditTag_RenameAndRecolor_PersistsAcrossPages` | `Settings/SettingsTests.cs` | Rename via inline edit row, assert original gone, renamed visible |
| E2E-TAG-002 | `Settings_EditTag_DuplicateName_ShowsInlineError` | `Settings/SettingsTests.cs` | Try renaming B → A; assert "already exists" in page; row stays open |
| E2E-TAG-003 | `Settings_EditTag_KeyboardOnly_CompletesEdit` | `Settings/SettingsTests.cs` | Focus pencil → Enter → type → Enter; renamed visible |

> All Playwright tests in this suite require an app running at `http://localhost:5125`. Run via `dotnet run --project src` in one terminal and `dotnet test tests/TaskPilot.Tests.E2E` in another. Smoke tests under `Smoke/DeploymentSmokeTests.cs` follow the same pattern with `SMOKE_BASE_URL`.

---

## 19. v1.12 — Tasks Page `show=` Filter (Active / Completed / All)

**Shipped in v1.12.** The `?incomplete=true` chip was removed and replaced by a Bootstrap segmented control (`Active` / `Completed` / `All`) driven by `?show=active|completed|all` (default: `active`). Filter state persists via `sessionStorage` (key `tp_tasks_filter`).

### 19.1 Integration Tests — Tasks Razor Page (`Tasks/TasksShowParamTests.cs`)

Tests hit the Razor Page `/tasks` endpoint (not the REST API) with cookie auth and verify HTML content.

| # | Test Name | Scenario | Expected |
|---|-----------|----------|----------|
| I-SHW-001 | `GetTasksPage_NoParams_DefaultsToActiveStatusesOnly` | 5 tasks (one per status); GET `/tasks` | Active tasks (0/1/2) appear; Completed/Cancelled do not |
| I-SHW-002 | `GetTasksPage_ShowCompleted_ReturnsOnlyTerminalStatuses` | 5 tasks; GET `/tasks?show=completed` | Done/Cancelled appear; NotStarted/InProgress/Blocked do not |
| I-SHW-003 | `GetTasksPage_ShowAll_ReturnsAllFiveStatuses` | 5 tasks; GET `/tasks?show=all` | All five task titles appear in HTML |
| I-SHW-004 | `GetTasksPage_ShowActiveAndOverdue_ReturnsOnlyOverdueActiveTasks` | overdue/future/no-date mix; GET `/tasks?show=active&overdue=true` | Only overdue active task appears |
| I-SHW-005 | `GetTasksPage_ShowActiveAndPriority_ReturnsOnlyMatchingPriorityActiveTasks` | Critical active + Critical completed + Medium active; GET `/tasks?show=active&priority=3` | Only Critical-active task appears |
| I-SHW-006 | `GetTasksPage_UnknownShowParam_TreatedAsActive` | 5 tasks; GET `/tasks?show=bogus` | Treated as active: no terminal statuses |

### 19.2 E2E Tests — `show=` Segmented Control and sessionStorage Persistence (`Tasks/TaskLifecycleTests.cs`)

These tests implement E2E-IV-003 through E2E-IV-013 from the 18.7 table above. E2E-IV-008 was renamed to `TasksPage_ShowSegment_Switching_UpdatesUrl` (covers segment cycling, not just the overdue chip).

**Deviations from spec:**
- E2E-IV-008: Original spec described Overdue chip toggle (which exists as `TasksPage_OverdueChip_TogglesAndUpdatesUrl`). Renamed to test segmented control switching to avoid collision.
- E2E-IV-011 back-button assertion: uses URL-contains check rather than exact navigation because `location.replace()` removes the bare `/tasks` entry; the exact resulting URL depends on navigation history order.
- E2E-IV-012 sessionStorage assertion: after reset, page re-saves `show=active` to sessionStorage automatically; test asserts old non-default value (`show=completed`) is absent, not that key is null.

**Existing tests updated for v1.12 (old `?incomplete=true` UI removed):**
- `TasksPage_IncompleteView_FiltersOutCompletedAndCancelled` — updated to use `?show=active` (marked `[Obsolete]`)
- `TasksPage_IncompleteChip_TogglesAndUpdatesUrl` — updated to use segmented control (marked `[Obsolete]`)
- `TasksPage_BoardViewWithIncompleteChip_HidesCompletedAndCancelledColumns` — updated to assert 3-column board at default show=active (marked `[Obsolete]`)
- `TasksPage_ColumnHeaderSort_PreservesActiveFilters` — updated `?incomplete=true` to `?show=active` in seed URL
- `Dashboard_IncompleteCard_NavigatesToFilteredTasksView` — updated URL assertion to `?show=active&status=NotStarted`
- `Dashboard_OverdueCard_NavigatesToOverdueIncompleteView` — updated URL assertion to `?show=active&overdue=true`

### 19.3 Security Validation — sessionStorage Round-trip

| Check | Finding |
|-------|---------|
| XSS via sessionStorage → location.replace | **PASS** — `location.replace('/tasks?' + saved)` cannot execute JS; value is appended as a URL query string, not injected into DOM or eval'd |
| XSS via sessionStorage → setAttribute href | **PASS** — sidebar rewrite uses `link.setAttribute('href', '/tasks?' + saved)`; prefix `/tasks?` ensures same-origin URL |
| sessionStorage value injection | **PASS** — save script reads only curated keys via `URLSearchParams`; values cannot inject new keys outside the allowed set |
| Server-side `show` param injection | **PASS** — `show` is switch-validated on the server; unrecognized values default to "active"; no SQL or reflection involved |

---

## 20. v1.13 — Clone Task (`POST /api/v1/tasks/{id}/clone`)

**Shipped in v1.13 (in flight).** A new endpoint duplicates any owned task, resets its status to `NotStarted`, and writes a single `ActivityLog` entry on the clone. The UX affordance is a one-click `bi-files` button on list rows (desktop/tablet ≥ 768 px) and on the Task Detail header (all breakpoints); after success the browser navigates to `/tasks/{newId}`.

Architecture spec: `ARCHITECTURE.md §3.1b`. UX spec: `WIREFRAMES.md` Pages 3 & 6. Interaction model: `USER-FLOWS.md` Flow 19 (19a list, 19b detail, 19c keyboard).

Test file targets:
- Unit: `tests/TaskPilot.Tests.Unit/Services/TaskServiceCloneTests.cs` (new file)
- Validator unit: `tests/TaskPilot.Tests.Unit/Validators/CloneTaskRequestValidatorTests.cs` (new file)
- Integration: `tests/TaskPilot.Tests.Integration/Tasks/CloneTaskEndpointTests.cs` (new file)
- E2E: `tests/TaskPilot.Tests.E2E/Tasks/CloneTaskTests.cs` (new file)

---

### 20.1 Unit Tests — `TaskService.CloneTaskAsync`

**Project:** `TaskPilot.Tests.Unit/Services/TaskServiceCloneTests.cs`
**Setup:** Moq + SQLite in-memory EF. Each test creates its own `DbContext` and `TaskService` instance. Helper factory builds a fully-populated `TaskItem` with tags, activityLogs, recurrence fields, and a non-null `CompletedDate`/`ResultAnalysis` on demand.

#### Happy path — field mapping

| # | Test Name | Arrange | Act | Assert |
|---|-----------|---------|-----|--------|
| U-CL-T-001 | `CloneTaskAsync_HappyPath_ReturnsNonNullResponse` | Source task in DB, valid userId | `CloneTaskAsync(sourceId, new CloneTaskRequest(), userId, modifiedBy)` | Returns non-null `TaskResponse` |
| U-CL-T-002 | `CloneTaskAsync_HappyPath_NewIdDiffersFromSource` | Source task in DB | `CloneTaskAsync` | `result.Id != sourceId` |
| U-CL-T-003 | `CloneTaskAsync_HappyPath_DefaultTitleHasCopySuffix` | Source `Title = "Prepare slides"`, no `Title` override | `CloneTaskAsync(…, new CloneTaskRequest())` | `result.Title == "Prepare slides (copy)"` |
| U-CL-T-004 | `CloneTaskAsync_HappyPath_DescriptionCopiedVerbatim` | Source `Description = "Some notes"` | `CloneTaskAsync` | `result.Description == "Some notes"` |
| U-CL-T-005 | `CloneTaskAsync_HappyPath_TaskTypeIdCopiedVerbatim` | Source has `TaskTypeId = 3` | `CloneTaskAsync` | `result.TaskTypeId == 3` |
| U-CL-T-006 | `CloneTaskAsync_HappyPath_AreaCopiedVerbatim` | Source `Area = Work` | `CloneTaskAsync` | `result.Area == Work` |
| U-CL-T-007 | `CloneTaskAsync_HappyPath_PriorityCopiedVerbatim` | Source `Priority = High` | `CloneTaskAsync` | `result.Priority == High` |
| U-CL-T-008 | `CloneTaskAsync_HappyPath_StatusForcedToNotStarted` | Source `Status = InProgress` | `CloneTaskAsync` | `result.Status == NotStarted` |
| U-CL-T-009 | `CloneTaskAsync_SourceCompleted_StatusForcedToNotStarted` | Source `Status = Completed`, `CompletedDate` set | `CloneTaskAsync` | `result.Status == NotStarted` |
| U-CL-T-010 | `CloneTaskAsync_SourceCancelled_StatusForcedToNotStarted` | Source `Status = Cancelled` | `CloneTaskAsync` | `result.Status == NotStarted` |
| U-CL-T-011 | `CloneTaskAsync_HappyPath_CompletedDateIsNull` | Source `CompletedDate = DateTime.UtcNow` | `CloneTaskAsync` | `result.CompletedDate == null` |
| U-CL-T-012 | `CloneTaskAsync_HappyPath_ResultAnalysisIsNull` | Source `ResultAnalysis = "Went well"` | `CloneTaskAsync` | `result.ResultAnalysis == null` |
| U-CL-T-013 | `CloneTaskAsync_HappyPath_IsDeletedFalseOnClone` | Source `IsDeleted = false` | `CloneTaskAsync` | Clone entity `IsDeleted == false` (verify in DB) |
| U-CL-T-014 | `CloneTaskAsync_HappyPath_RecurrencePatternCopied` | Source `IsRecurring = true`, `RecurrencePattern = Weekly` | `CloneTaskAsync` | `result.IsRecurring == true`, `result.RecurrencePattern == Weekly` |
| U-CL-T-015 | `CloneTaskAsync_SourceNotRecurring_RecurrencePatternNullOnClone` | Source `IsRecurring = false`, `RecurrencePattern = null` | `CloneTaskAsync` | `result.IsRecurring == false`, `result.RecurrencePattern == null` |

#### Target-date handling

| # | Test Name | Arrange | Act | Assert |
|---|-----------|---------|-----|--------|
| U-CL-T-016 | `CloneTaskAsync_NeitherOverrideNorClear_TargetDateCopiedVerbatim` | Source `TargetDate = May 30`, `CloneTaskRequest()` default | `CloneTaskAsync` | `result.TargetDate == May 30` |
| U-CL-T-017 | `CloneTaskAsync_TargetDateOverride_UsesOverrideDate` | Source `TargetDate = May 30`, request `TargetDate = June 15`, `ClearTargetDate = false` | `CloneTaskAsync` | `result.TargetDate == June 15` |
| U-CL-T-018 | `CloneTaskAsync_ClearTargetDateTrue_TargetDateIsNull` | Source `TargetDate = May 30`, request `ClearTargetDate = true` | `CloneTaskAsync` | `result.TargetDate == null` |
| U-CL-T-019 | `CloneTaskAsync_ClearTargetDateTrueWithOverride_OverrideIgnoredDateIsNull` | Source `TargetDate = May 30`, request `TargetDate = June 15`, `ClearTargetDate = true` | `CloneTaskAsync` | `result.TargetDate == null` (clear flag wins) |
| U-CL-T-020 | `CloneTaskAsync_SourceHasNoTargetDate_CloneAlsoHasNone` | Source `TargetDate = null`, default request | `CloneTaskAsync` | `result.TargetDate == null` |
| U-CL-T-021 | `CloneTaskAsync_TargetDateTypeCopiedVerbatim` | Source `TargetDateType = ThisWeek` | `CloneTaskAsync` | `result.TargetDateType == ThisWeek` |

#### Title override

| # | Test Name | Arrange | Act | Assert |
|---|-----------|---------|-----|--------|
| U-CL-T-022 | `CloneTaskAsync_TitleOverrideSupplied_UsesOverride` | Source `Title = "Foo"`, request `Title = "Bar"` | `CloneTaskAsync` | `result.Title == "Bar"` |
| U-CL-T-023 | `CloneTaskAsync_TitleOverrideNull_UsesCopySuffix` | Source `Title = "Foo"`, request `Title = null` | `CloneTaskAsync` | `result.Title == "Foo (copy)"` |
| U-CL-T-024 | `CloneTaskAsync_TitleOverrideWhitespaceOnly_UsesCopySuffix` | Source `Title = "Foo"`, request `Title = "   "` | `CloneTaskAsync` | `result.Title == "Foo (copy)"` (whitespace treated as absent) |
| U-CL-T-025 | `CloneTaskAsync_TitleOverrideEmptyString_UsesCopySuffix` | Source `Title = "Foo"`, request `Title = ""` | `CloneTaskAsync` | `result.Title == "Foo (copy)"` |
| U-CL-T-026 | `CloneTaskAsync_DoubleClone_ProducesCopyCopySuffix` | Source `Title = "Foo (copy)"`, no override | `CloneTaskAsync` | `result.Title == "Foo (copy) (copy)"` (no deduplication in v1) |

#### Tags

| # | Test Name | Arrange | Act | Assert |
|---|-----------|---------|-----|--------|
| U-CL-T-027 | `CloneTaskAsync_TagsCopied_CountMatchesSource` | Source has 3 tags | `CloneTaskAsync` | Clone in DB has 3 `TaskTag` rows |
| U-CL-T-028 | `CloneTaskAsync_TagsCopied_TagIdsMatchSource` | Source has tags T1, T2 | `CloneTaskAsync` | Clone's `TaskTag.TagId` set = `{T1, T2}` |
| U-CL-T-029 | `CloneTaskAsync_SourceHasNoTags_CloneHasNoTags` | Source has 0 tags | `CloneTaskAsync` | Clone has 0 `TaskTag` rows |

#### SortOrder

| # | Test Name | Arrange | Act | Assert |
|---|-----------|---------|-----|--------|
| U-CL-T-030 | `CloneTaskAsync_SortOrderIsMaxPlusOne` | User has 3 tasks with SortOrder 1, 2, 3 | `CloneTaskAsync` | Clone `SortOrder == 4` |
| U-CL-T-031 | `CloneTaskAsync_NoExistingTasks_SortOrderIsOne` | User has no other tasks | `CloneTaskAsync` | Clone `SortOrder == 1` (or whatever `GetMaxSortOrderAsync` returns for an empty set + 1) |

#### ActivityLog

| # | Test Name | Arrange | Act | Assert |
|---|-----------|---------|-----|--------|
| U-CL-T-032 | `CloneTaskAsync_ActivityLog_ExactlyOneEntryOnClone` | Source task exists | `CloneTaskAsync` | Clone has exactly 1 `TaskActivityLog` row |
| U-CL-T-033 | `CloneTaskAsync_ActivityLog_FieldChangedIsCreated` | Source task exists | `CloneTaskAsync` | Clone's log entry `FieldChanged == "Created"` |
| U-CL-T-034 | `CloneTaskAsync_ActivityLog_NewValueContainsSourceId` | Source `Id = {guid}` | `CloneTaskAsync` | Clone's log `NewValue == $"Cloned from {sourceId:D}"` (lowercase 36-char GUID) |
| U-CL-T-035 | `CloneTaskAsync_ActivityLog_OldValueIsNull` | Source task exists | `CloneTaskAsync` | Clone's log entry `OldValue == null` |
| U-CL-T-036 | `CloneTaskAsync_ActivityLog_ChangedByIsModifiedBy` | `modifiedBy = "user:alice"` | `CloneTaskAsync` | Clone's log `ChangedBy == "user:alice"` |
| U-CL-T-037 | `CloneTaskAsync_ActivityLog_ApiCaller_ChangedByIsApiKeyName` | `modifiedBy = "api:Claude-Work"` | `CloneTaskAsync` | Clone's log `ChangedBy == "api:Claude-Work"` |

#### Source immutability

| # | Test Name | Arrange | Act | Assert |
|---|-----------|---------|-----|--------|
| U-CL-T-038 | `CloneTaskAsync_SourceTask_NoActivityLogAdded` | Source has 1 existing log entry | `CloneTaskAsync` | Source still has exactly 1 log entry after operation |
| U-CL-T-039 | `CloneTaskAsync_SourceTask_LastModifiedDateUnchanged` | Record source `LastModifiedDate` before clone | `CloneTaskAsync` | Source `LastModifiedDate` unchanged (within 1ms tolerance) |
| U-CL-T-040 | `CloneTaskAsync_SourceTask_StatusUnchanged` | Source `Status = Completed` | `CloneTaskAsync` | Source `Status` still `Completed` after clone |

#### 404 / authorization

| # | Test Name | Arrange | Act | Assert |
|---|-----------|---------|-----|--------|
| U-CL-T-041 | `CloneTaskAsync_MissingId_ReturnsNull` | No task with given `sourceId` | `CloneTaskAsync` | Returns `null` |
| U-CL-T-042 | `CloneTaskAsync_SoftDeletedSource_ReturnsNull` | Source has `IsDeleted = true` | `CloneTaskAsync` | Returns `null` (global query filter excludes it) |
| U-CL-T-043 | `CloneTaskAsync_CrossUserSource_ReturnsNull` | Source `UserId = userA`; call with `userId = userB` | `CloneTaskAsync` | Returns `null` |

#### LastModifiedBy

| # | Test Name | Arrange | Act | Assert |
|---|-----------|---------|-----|--------|
| U-CL-T-044 | `CloneTaskAsync_LastModifiedBy_UserCaller_FormattedCorrectly` | `modifiedBy = "user:alice"` | `CloneTaskAsync` | Clone `LastModifiedBy == "user:alice"` |
| U-CL-T-045 | `CloneTaskAsync_LastModifiedBy_ApiKeyCaller_FormattedCorrectly` | `modifiedBy = "api:MyKey"` | `CloneTaskAsync` | Clone `LastModifiedBy == "api:MyKey"` |

---

### 20.2 Unit Tests — `CloneTaskRequestValidator`

**Project:** `TaskPilot.Tests.Unit/Validators/CloneTaskRequestValidatorTests.cs`

| # | Test Name | Input | Expected |
|---|-----------|-------|----------|
| U-CL-V-001 | `Validate_EmptyRequest_Passes` | `new CloneTaskRequest()` (all defaults) | Valid — no errors |
| U-CL-V-002 | `Validate_EmptyBody_Passes` | `{}` deserialized to default `CloneTaskRequest` | Valid |
| U-CL-V-003 | `Validate_TitleNull_Passes` | `Title = null` | Valid (null means "use default") |
| U-CL-V-004 | `Validate_TitleWhitespaceOnly_Passes` | `Title = "   "` | Valid (whitespace treated as absent by service; validator does not reject it) |
| U-CL-V-005 | `Validate_TitleExactly200Chars_Passes` | `Title` = 200-char string | Valid |
| U-CL-V-006 | `Validate_Title201Chars_FailsWithMessage` | `Title` = 201-char string | Invalid, error on `Title`: "Title must not exceed 200 characters" |
| U-CL-V-007 | `Validate_ClearTargetDateFalse_Passes` | `ClearTargetDate = false` | Valid |
| U-CL-V-008 | `Validate_ClearTargetDateTrue_Passes` | `ClearTargetDate = true` | Valid |
| U-CL-V-009 | `Validate_TargetDateSupplied_ClearFalse_Passes` | `TargetDate = DateTime.UtcNow.AddDays(7)`, `ClearTargetDate = false` | Valid |
| U-CL-V-010 | `Validate_TargetDateSupplied_ClearTrue_Passes` | `TargetDate = DateTime.UtcNow`, `ClearTargetDate = true` | Valid (conflict resolved by service, not validator) |

---

### 20.3 Integration Tests — `POST /api/v1/tasks/{id}/clone`

**Project:** `TaskPilot.Tests.Integration/Tasks/CloneTaskEndpointTests.cs`
**Setup:** `WebApplicationFactory<Program>` with fresh SQLite in-memory DB per test class. Helpers: `CreateCookieAuthClient()`, `CreateApiKeyClient(string name)`, `SeedTask(HttpClient client, ...)`.

#### Happy path

| # | Test Name | Setup | Request | Expected |
|---|-----------|-------|---------|----------|
| I-CL-001 | `CloneTask_ValidId_Returns201` | Seed 1 task, cookie auth | POST `/api/v1/tasks/{id}/clone` empty body `{}` | 201 Created |
| I-CL-002 | `CloneTask_ValidId_ResponseIsStandardEnvelope` | Seed 1 task | POST clone | Body is `ApiResponse<TaskResponse>`; `data` present; `meta.requestId` is non-empty GUID |
| I-CL-003 | `CloneTask_ValidId_LocationHeaderPointsToNewTask` | Seed 1 task | POST clone | `Location` header = `/api/v1/tasks/{newId}` |
| I-CL-004 | `CloneTask_ValidId_NewTaskExistsInDb` | Seed 1 task | POST clone | GET `/api/v1/tasks/{newId}` returns 200 |
| I-CL-005 | `CloneTask_DefaultTitle_HasCopySuffix` | Seed task `Title = "My Task"`, empty body | POST clone | `data.title == "My Task (copy)"` |
| I-CL-006 | `CloneTask_TitleOverride_UsesProvidedTitle` | Seed task, body `{ "title": "New Title" }` | POST clone | `data.title == "New Title"` |
| I-CL-007 | `CloneTask_StatusIsAlwaysNotStarted` | Seed `Status = InProgress` task | POST clone | `data.status == "NotStarted"` |
| I-CL-008 | `CloneTask_CompletedSource_StatusIsNotStarted` | Seed `Status = Completed` task | POST clone | `data.status == "NotStarted"` |
| I-CL-009 | `CloneTask_CompletedSource_CompletedDateIsNull` | Seed completed task with `CompletedDate` set | POST clone | `data.completedDate == null` |
| I-CL-010 | `CloneTask_CompletedSource_ResultAnalysisIsNull` | Seed completed task with `ResultAnalysis = "text"` | POST clone | `data.resultAnalysis == null` |
| I-CL-011 | `CloneTask_TagsCopiedToClone` | Seed task with 2 tags | POST clone | `data.tags` array length = 2; tag names match source |
| I-CL-012 | `CloneTask_TargetDateCopiedWhenNoOverride` | Seed task `TargetDate = "2026-06-01"` | POST clone empty body | `data.targetDate == "2026-06-01"` |
| I-CL-013 | `CloneTask_TargetDateOverride_AppliesOverride` | Seed task `TargetDate = "2026-06-01"`, body `{ "targetDate": "2026-07-15" }` | POST clone | `data.targetDate == "2026-07-15"` |
| I-CL-014 | `CloneTask_ClearTargetDate_SetsNull` | Seed task `TargetDate = "2026-06-01"`, body `{ "clearTargetDate": true }` | POST clone | `data.targetDate == null` |
| I-CL-015 | `CloneTask_SourceHasNoTargetDate_CloneAlsoHasNone` | Seed task `TargetDate = null` | POST clone empty body | `data.targetDate == null` |
| I-CL-016 | `CloneTask_ActivityLog_OneEntryOnClone` | Seed 1 task | POST clone | GET `/api/v1/tasks/{newId}/activity` → `data` has exactly 1 entry |
| I-CL-017 | `CloneTask_ActivityLog_FieldChangedIsCreated` | Seed 1 task | POST clone | Activity log entry `fieldChanged == "Created"` |
| I-CL-018 | `CloneTask_ActivityLog_NewValueContainsSourceId` | Seed task with known `id` | POST clone | Activity log `newValue == $"Cloned from {sourceId:D}"` |
| I-CL-019 | `CloneTask_SourceTask_ActivityLogUnchanged` | Seed task with 1 pre-existing log entry | POST clone | GET source activity log → still exactly 1 entry |
| I-CL-020 | `CloneTask_EmptyBody_IsAccepted` | Seed 1 task | POST with no body (`Content-Length: 0`) | 201 Created |
| I-CL-021 | `CloneTask_RecurringSource_RecurrenceCopied` | Seed `IsRecurring = true`, `RecurrencePattern = Weekly` | POST clone | `data.isRecurring == true`, `data.recurrencePattern == "Weekly"` |

#### Auth

| # | Test Name | Setup | Request | Expected |
|---|-----------|-------|---------|----------|
| I-CL-022 | `CloneTask_Unauthenticated_Returns401` | No auth client | POST clone | 401, standard error envelope |
| I-CL-023 | `CloneTask_CookieAuth_Returns201` | Cookie-authenticated client | POST clone | 201 |
| I-CL-024 | `CloneTask_ApiKeyAuth_Returns201` | X-Api-Key authenticated client | POST clone | 201 |
| I-CL-025 | `CloneTask_ApiKeyAuth_LastModifiedByIsApiKeyName` | API key named "Claude-Work" | POST clone | `data.lastModifiedBy == "api:Claude-Work"` |
| I-CL-026 | `CloneTask_CookieAuth_LastModifiedByIsUsername` | Cookie auth as "testuser" | POST clone | `data.lastModifiedBy == "user:testuser"` |

#### Errors

| # | Test Name | Setup | Request | Expected |
|---|-----------|-------|---------|----------|
| I-CL-027 | `CloneTask_MissingId_Returns404` | No task with given ID | POST `/api/v1/tasks/{randomGuid}/clone` | 404, `error.code == "NOT_FOUND"` |
| I-CL-028 | `CloneTask_SoftDeletedSource_Returns404` | Seed task, then DELETE it | POST clone against original ID | 404, `error.code == "NOT_FOUND"` |
| I-CL-029 | `CloneTask_SoftDeletedSource_SameBodyAsMissing` | Seed + delete task | Compare 404 body from I-CL-028 to I-CL-027 | Response body structure and `error.code` are identical (no info disclosure) |
| I-CL-030 | `CloneTask_AnotherUsersTask_Returns404` | Seed task for user A; authenticate as user B | POST clone | 404 (not 403 — no cross-user data leakage) |
| I-CL-031 | `CloneTask_TitleExceeds200Chars_Returns400` | Valid task | POST clone body `{ "title": "<201-char string>" }` | 400, `error.code == "VALIDATION_ERROR"`, `details` contains `title` field error |

#### Audit middleware

| # | Test Name | Setup | Request | Expected |
|---|-----------|-------|---------|----------|
| I-CL-032 | `CloneTask_ApiKeyAuth_AuditLogEntryCreated` | API key auth | POST clone (success) | GET `/api/v1/audit` → new entry with `method = "POST"` and path containing `/clone` |
| I-CL-033 | `CloneTask_CookieAuth_NoAuditLogEntry` | Cookie auth | POST clone (success) | `ApiAuditLog` count unchanged (cookie auth not audited) |

#### Atomicity

| # | Test Name | Setup | Request | Expected |
|---|-----------|-------|---------|----------|
| I-CL-034 | `CloneTask_SaveFailure_NoPartialRowsInDb` | Inject a `DbContext` wrapper that throws after entity attach but before `SaveChangesAsync` completes (e.g., by seeding a tag with a duplicate-key constraint that fires mid-transaction, or by replacing `ApplicationDbContext` with a subclass that overrides `SaveChangesAsync` to throw) | POST clone | 500 response; zero new `TaskItem` rows; zero new `TaskTag` rows; zero new `TaskActivityLog` rows for the attempted clone |

_Implementation note for fullstack-dev: the recommended injection point is a test-specific `ApplicationDbContext` subclass registered via `WebApplicationFactory.WithWebHostBuilder` that overrides `SaveChangesAsync` to throw an `InvalidOperationException` on first call when a feature flag (e.g., `IOptions<TestOptions>.ThrowOnSave`) is set. This mirrors the pattern used by other atomicity tests in the codebase._

---

### 20.4 E2E Tests — Clone Task (`Tasks/CloneTaskTests.cs`)

**Project:** `TaskPilot.Tests.E2E/Tasks/CloneTaskTests.cs`
**Tag:** `[Trait("Category", "E2E")]`
**Pattern:** xUnit `[Collection("Playwright")]`, shared `PlaywrightFixture`. Each test authenticates, seeds a task, then exercises the clone affordance.
**Verbatim toast strings (assert these exactly):**
- Success: `"Task cloned. You're now viewing the copy."`
- 404 error: `"This task can't be cloned. It may have been deleted."`
- 5xx/network error: `"Couldn't clone the task. Please try again."`

#### Desktop / Tablet — Tasks list (viewport ≥ 768 px)

| # | Test Name | Steps | Expected |
|---|-----------|-------|----------|
| E-CL-001 | `TaskList_Desktop_CloneButton_IsVisible` | Authenticate at 1280 px; navigate to `/tasks`; create a task | `.btn[aria-label^="Clone task"]` or `button[aria-label^="Clone task"]` is visible in the list row |
| E-CL-002 | `TaskList_Desktop_CloneRow_NavigatesToNewTask` | Create task "Orignal Task"; click Clone on its row | URL changes to `/tasks/{newId}` (not the source ID) |
| E-CL-003 | `TaskList_Desktop_CloneRow_TitleHasCopySuffix` | Create task "Alpha Task"; click Clone on its row | New Detail page `h1` or title field contains `"Alpha Task (copy)"` |
| E-CL-004 | `TaskList_Desktop_CloneRow_StatusPillIsNotStarted` | Create InProgress task; click Clone | New task's status indicator shows "Not Started" (or "NotStarted") |
| E-CL-005 | `TaskList_Desktop_CloneRow_TagsInherited` | Create task with tag "urgent"; click Clone | New Detail page shows tag "urgent" |
| E-CL-006 | `TaskList_Desktop_CloneRow_SuccessToastShownVerbatim` | Create task; click Clone | Toast with text `"Task cloned. You're now viewing the copy."` is visible (success variant — green) |

#### Detail page (all breakpoints)

| # | Test Name | Steps | Expected |
|---|-----------|-------|----------|
| E-CL-007 | `TaskDetail_CloneButton_AlwaysRendered` | Navigate to any task detail page (desktop 1280 px) | `button[aria-label^="Clone task"]` or equivalent is present in the header action row |
| E-CL-008 | `TaskDetail_CloneButton_RenderedForCompletedTask` | Complete a task; navigate to its detail page | Clone button is still rendered (not hidden for terminal-status tasks) |
| E-CL-009 | `TaskDetail_CloneButton_NavigatesToNewTask` | Navigate to task detail; click Clone | URL changes to `/tasks/{newId}` |
| E-CL-010 | `TaskDetail_CloneButton_SuccessToastShownVerbatim` | Navigate to task detail; click Clone | Toast text is exactly `"Task cloned. You're now viewing the copy."` |
| E-CL-011 | `TaskDetail_CloneCompletedTask_CloneShowsNotStarted` | Complete task; navigate to detail; click Clone | New task page shows "Not Started" status, not "Completed" |

#### Keyboard-only path (Flow 19c)

| # | Test Name | Steps | Expected |
|---|-----------|-------|----------|
| E-CL-012 | `TaskList_KeyboardOnly_TabToCloneAndActivate` | Navigate to `/tasks`; Tab through list row to reach Clone button; press Enter | Page navigates to new task URL; no mouse interaction used |
| E-CL-013 | `TaskDetail_KeyboardOnly_TabToCloneAndActivate` | Navigate to `/tasks/{id}`; Tab to Clone button in header; press Enter | Page navigates to `/tasks/{newId}` |
| E-CL-014 | `TaskDetail_KeyboardOnly_FocusLandsOnH1AfterNavigation` | Navigate to detail; Tab to Clone; Enter | After navigation, focused element is the new Detail page's `<h1>` or title heading (`document.activeElement` is within the heading) |

#### Mobile viewport (≤ 640 px)

| # | Test Name | Steps | Expected |
|---|-----------|-------|----------|
| E-CL-015 | `TaskList_Mobile_CloneButtonNotRendered` | Authenticate at 390 px viewport; navigate to `/tasks` | No `button[aria-label^="Clone task"]` visible in task list rows (clone is mobile detail-only) |
| E-CL-016 | `TaskDetail_Mobile_CloneButtonIsRendered` | Authenticate at 390 px; navigate to a task's detail page | Clone button is present in the Detail header action row |
| E-CL-017 | `TaskDetail_Mobile_CloneNavigatesCorrectly` | 390 px viewport; Detail page; click Clone | URL changes to `/tasks/{newId}` |

#### Error states

| # | Test Name | Steps | Expected |
|---|-----------|-------|----------|
| E-CL-018 | `CloneTask_404Error_ShowsVerboseToastAndNoNavigation` | Seed a task; delete it; using the stale source ID attempt a clone (e.g., via the second-tab pattern: Tab A has the Detail page open before deletion; Tab B deletes; Tab A clicks Clone) | Toast text is exactly `"This task can't be cloned. It may have been deleted."`; URL remains on the pre-clone page; Clone button is re-enabled after toast |
| E-CL-019 | `CloneTask_404Error_ToastDismissesAfter8Seconds` | Trigger 404 error state | Toast is visible immediately; assert toast no longer visible after 9 s (8 s auto-dismiss) |
| E-CL-020 | `CloneTask_CompletedSource_SucceedsAndShowsNotStarted` | Create and complete a task; navigate to `/tasks`; click Clone on the completed task | No error; navigates to `/tasks/{newId}`; clone shows "Not Started" status |

---

## Coverage Targets

| Layer | Target | How to measure |
|-------|--------|----------------|
| Service methods | >80% | `dotnet test --collect:"XPlat Code Coverage"` |
| Controller actions | >80% | Same |
| Repository methods | >70% | Same |
| Validators | 100% | Every rule tested |
| E2E flows | All 12 user flows covered | Manual map to E2E test cases |
