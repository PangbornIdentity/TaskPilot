---
id: FR-TASKS-026
type: FR
area: TASKS
provenance: test
status: unratified
verification: automated
why: Falling back to the default Active view on a bad show value prevents a broken or empty page from a malformed URL.
tests: ["tests/TaskPilot.Tests.Integration/Tasks/TasksShowParamTests.cs"]
---
An unrecognized show value on the Tasks page is treated as the default Active view.
