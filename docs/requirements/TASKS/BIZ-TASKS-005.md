---
id: BIZ-TASKS-005
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: Logging the status transition on completion lets users and auditors see when and from what state a task was completed.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs"]
---
Completing a task writes a "Status" activity-log entry recording the task's prior status as the old value and "Completed" as the new value.
