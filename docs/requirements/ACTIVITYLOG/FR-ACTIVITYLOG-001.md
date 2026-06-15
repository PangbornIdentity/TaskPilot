---
id: FR-ACTIVITYLOG-001
type: FR
area: ACTIVITYLOG
provenance: test
status: unratified
verification: automated
why: Scoping change history to the requesting user and paginating it prevents cross-user leakage and keeps long histories navigable.
tests: ["tests/TaskPilot.Tests.Integration/ActivityLogs/ActivityLogApiTests.cs", "tests/TaskPilot.Tests.Unit/Services/ActivityLogServiceTests.cs"]
---
GET /api/v1/activity-logs returns the user's task change-history entries as a paginated list honoring page and pageSize parameters, returning an empty list with zero total count for a user with no activity.
