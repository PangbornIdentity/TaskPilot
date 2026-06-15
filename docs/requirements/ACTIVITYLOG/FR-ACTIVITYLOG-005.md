---
id: FR-ACTIVITYLOG-005
type: FR
area: ACTIVITYLOG
provenance: test
status: unratified
verification: automated
why: SUGGESTED: These fields are what the activity-log view renders per entry — old/new values and who changed them make each change self-describing.
tests: ["tests/TaskPilot.Tests.Integration/ActivityLogs/ActivityLogApiTests.cs"]
---
Each activity-log entry exposes id, taskId, taskTitle, timestamp, fieldChanged, oldValue, newValue, and changedBy fields.
