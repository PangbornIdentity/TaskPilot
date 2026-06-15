---
id: FR-ACTIVITYLOG-002
type: FR
area: ACTIVITYLOG
provenance: test
status: unratified
verification: automated
why: The taskId filter powers the per-task activity log on the task detail view, so it must return only that task's history.
tests: ["tests/TaskPilot.Tests.Integration/ActivityLogs/ActivityLogApiTests.cs", "tests/TaskPilot.Tests.Unit/Services/ActivityLogServiceTests.cs"]
---
GET /api/v1/activity-logs accepts a taskId filter and returns only the change-history entries belonging to that task.
