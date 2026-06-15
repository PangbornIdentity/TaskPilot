---
id: FR-TASKS-007
type: FR
area: TASKS
provenance: test
status: unratified
verification: automated
why: Full update lets users edit any editable field of an existing task and persist the result.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/TasksApiTests.cs"]
---
A user can fully update an existing task's editable fields, and the updated values are persisted and returned.
