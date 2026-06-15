---
id: BIZ-TASKS-008
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: The incomplete filter exists so users can focus on actionable work, excluding finished or abandoned tasks.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskRepositoryFilterTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/TasksApiTests.cs"]
---
The incomplete-only filter returns exactly the tasks with status NotStarted, InProgress, or Blocked, excluding Completed and Cancelled tasks.
