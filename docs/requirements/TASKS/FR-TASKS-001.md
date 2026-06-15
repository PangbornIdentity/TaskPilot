---
id: FR-TASKS-001
type: FR
area: TASKS
provenance: test
status: unratified
verification: automated
why: Task creation is the core action of the app; capturing all task fields up front lets users record complete work items.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/TasksApiTests.cs"]
---
A user can create a task with a title, description, type, area, priority, status, target-date type, optional target date, recurrence settings, and optional tags, and the created task is persisted and returned.
