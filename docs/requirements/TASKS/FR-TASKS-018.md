---
id: FR-TASKS-018
type: FR
area: TASKS
provenance: test
status: unratified
verification: automated
why: Deleting from the detail page lets users remove a task while viewing it, without returning to the list first.
tests: ["tests/TaskPilot.Tests.E2E/Tasks/TaskLifecycleTests.cs"]
---
A user can delete a task from its detail page, after which it is removed from the task list.
