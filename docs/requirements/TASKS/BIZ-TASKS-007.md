---
id: BIZ-TASKS-007
type: BIZ
area: TASKS
provenance: human
status: ratified
verification: automated
why: SUGGESTED: A full update should record every genuine field change so the activity log stays a complete, trustworthy history.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs", "tests/TaskPilot.Tests.Unit/Services/UpdateTaskActivityLogTests.cs"]
---
A full task update writes one activity-log entry per changed field — including Title, Description, TaskTypeId, Area, Priority, Status, TargetDate, AND TargetDateType, IsRecurring, and RecurrencePattern; unchanged fields are not logged.
