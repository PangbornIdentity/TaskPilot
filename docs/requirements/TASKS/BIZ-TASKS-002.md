---
id: BIZ-TASKS-002
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: Completion time must be captured the moment a task reaches Completed so completion history and analytics are accurate; non-completed tasks must not carry a false completion time.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs"]
---
When a task is created with status Completed its completion timestamp is set; when created with any non-completed status the completion timestamp is null.
