---
id: FR-TASKS-030
type: FR
area: TASKS
provenance: test
status: unratified
verification: automated
why: A Reset Filters action gives users a one-click escape back to the default view when filters get confusing.
tests: ["tests/TaskPilot.Tests.E2E/Tasks/TaskLifecycleTests.cs"]
---
A Reset Filters action on the Tasks page clears the persisted filter state and returns the page to the default Active view.
