---
id: FR-TASKS-004
type: FR
area: TASKS
provenance: test
status: unratified
verification: automated
why: Completing a task with optional result analysis captures both the outcome and the user's reflection for later review.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/TasksApiTests.cs"]
---
A user can complete a task, which sets its status to Completed, records the completion timestamp, and optionally stores a result-analysis note.
