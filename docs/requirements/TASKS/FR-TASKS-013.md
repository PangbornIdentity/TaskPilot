---
id: FR-TASKS-013
type: FR
area: TASKS
provenance: test
status: unratified
verification: automated
why: The New Task modal is the primary UI path for creating tasks for web users.
tests: ["tests/TaskPilot.Tests.E2E/Tasks/TaskLifecycleTests.cs", "tests/TaskPilot.Tests.E2E/Tasks/TaskAreaTypeTagTests.cs"]
---
A user can create a task through the Tasks page UI (via the New Task modal), and the new task then appears in the task list.
