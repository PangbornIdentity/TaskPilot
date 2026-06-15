---
id: FR-TASKS-006
type: FR
area: TASKS
provenance: test
status: unratified
verification: automated
why: Soft-deleting and hiding a task lets users clear their list while preserving an audit trail and an undo window.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/TasksApiTests.cs"]
---
A user can delete a task, after which it no longer appears in listings or single-task retrieval.
