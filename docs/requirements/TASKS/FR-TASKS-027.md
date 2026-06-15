---
id: FR-TASKS-027
type: FR
area: TASKS
provenance: test
status: unratified
verification: automated
why: Composing the show segment with other filters lets users combine status focus with overdue and priority narrowing.
tests: ["tests/TaskPilot.Tests.Integration/Tasks/TasksShowParamTests.cs"]
---
The Tasks page show segment composes with other filters (overdue and priority), returning only tasks matching both the segment and the additional filter.
