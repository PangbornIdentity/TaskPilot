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

## Coverage Targets

| Layer | Target | How to measure |
|-------|--------|----------------|
| Service methods | >80% | `dotnet test --collect:"XPlat Code Coverage"` |
| Controller actions | >80% | Same |
| Repository methods | >70% | Same |
| Validators | 100% | Every rule tested |
| E2E flows | All 12 user flows covered | Manual map to E2E test cases |
