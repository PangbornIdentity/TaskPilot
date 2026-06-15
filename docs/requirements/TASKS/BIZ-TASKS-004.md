---
id: BIZ-TASKS-004
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: Recording deletions in the activity log preserves an audit trail given tasks are soft-deleted rather than hard-deleted.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs"]
---
Deleting a task writes a single activity-log entry whose field is "Deleted" and whose old value is the task title.
