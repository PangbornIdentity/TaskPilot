---
id: BIZ-TASKS-010
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: Composing filters conjunctively lets users narrow to exactly the slice they care about (for example work that is both unfinished and late).
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskRepositoryFilterTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/TasksApiTests.cs"]
---
The incomplete-only and overdue-only filters compose conjunctively, returning only tasks that are both incomplete and overdue.
