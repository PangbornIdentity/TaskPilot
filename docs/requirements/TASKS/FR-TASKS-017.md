---
id: FR-TASKS-017
type: FR
area: TASKS
provenance: test
status: unratified
verification: automated
why: The detail page is where users review and edit a task; logging edits keeps the change history complete.
tests: ["tests/TaskPilot.Tests.E2E/Tasks/TaskLifecycleTests.cs"]
---
A user can open a task's detail page to view and edit its fields, and editing a field then saving records the change in the task's change history.
