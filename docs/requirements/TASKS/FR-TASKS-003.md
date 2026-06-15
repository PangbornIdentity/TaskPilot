---
id: FR-TASKS-003
type: FR
area: TASKS
provenance: test
status: unratified
verification: automated
why: Single-task retrieval backs detail views and edit flows; not-found signals a missing or inaccessible task clearly.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/TasksApiTests.cs"]
---
A user can retrieve a single task by its id, receiving its full details, and a request for a non-existent task returns not-found.
