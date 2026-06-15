---
id: BIZ-TASKS-006
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: Logging only fields that actually changed keeps the activity log meaningful and avoids noise from no-op updates.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs"]
---
A partial update writes one activity-log entry per field that actually changes, recording the field's old and new values; fields whose value is unchanged produce no activity-log entry.
