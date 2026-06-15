---
id: FR-TASKS-008
type: FR
area: TASKS
provenance: human
status: ratified
verification: automated
why: Partial update lets clients and the API change selected fields without resending the whole task or risking unintended overwrites.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/TasksApiTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/PatchTaskValidationTests.cs"]
---
A user can partially update a task by supplying only selected fields; supplied fields are changed while omitted (null) fields retain their existing values.
