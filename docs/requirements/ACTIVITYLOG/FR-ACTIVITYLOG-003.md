---
id: FR-ACTIVITYLOG-003
type: FR
area: ACTIVITYLOG
provenance: test
status: unratified
verification: automated
why: Every write operation must leave an audit trail, so creates and updates each record a change-history entry.
tests: ["tests/TaskPilot.Tests.Integration/ActivityLogs/ActivityLogApiTests.cs"]
---
Creating a task records an activity-log entry with fieldChanged "Created", and updating a task records an activity-log entry for that task.
