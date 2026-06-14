---
id: FR-ACTIVITYLOG-004
type: FR
area: ACTIVITYLOG
provenance: test
status: unratified
verification: automated
why: Soft-delete preserves the record, so a deleted task's history must remain auditable rather than vanishing with the task.
tests: ["tests/TaskPilot.Tests.Integration/ActivityLogs/ActivityLogApiTests.cs"]
---
Deleting a task records an activity-log entry with fieldChanged "Deleted", and that entry (and the task's prior history) remains visible via the activity-log API even after the task is soft-deleted.
