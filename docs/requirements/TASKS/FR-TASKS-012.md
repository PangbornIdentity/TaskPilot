---
id: FR-TASKS-012
type: FR
area: TASKS
provenance: test
status: unratified
verification: automated
why: Cloning lets users start a new task from an existing one, saving re-entry for similar work.
tests: ["tests/TaskPilot.Tests.Integration/Tasks/CloneTaskEndpointTests.cs", "tests/TaskPilot.Tests.Unit/Services/TaskServiceCloneTests.cs"]
---
A user can clone an existing task, producing a new independent task whose creation is acknowledged with a location reference to the new task and which is immediately retrievable.
