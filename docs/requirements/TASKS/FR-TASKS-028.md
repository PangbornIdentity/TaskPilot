---
id: FR-TASKS-028
type: FR
area: TASKS
provenance: test
status: unratified
verification: automated
why: Matching board columns to the active segment keeps the kanban consistent with the chosen status scope.
tests: ["tests/TaskPilot.Tests.E2E/Tasks/TaskLifecycleTests.cs"]
---
The board (kanban) view renders columns matching the active show segment: three open-status columns for Active and the two terminal-status columns for Completed.
