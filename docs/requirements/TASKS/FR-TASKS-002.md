---
id: FR-TASKS-002
type: FR
area: TASKS
provenance: test
status: unratified
verification: automated
why: Allowing tags at creation lets users categorize a task immediately rather than in a follow-up edit.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/TasksApiTests.cs"]
---
A task can be created with one or more tags attached, and those tags are associated with the task and appear in its representation.
