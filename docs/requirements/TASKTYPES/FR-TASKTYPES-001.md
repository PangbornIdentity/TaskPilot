---
id: FR-TASKTYPES-001
type: FR
area: TASKTYPES
provenance: test
status: unratified
why: Clients need the canonical type list to populate the task-type dropdown and to map a task's TaskTypeId to a display name; positive ids and non-empty names guarantee each entry is selectable and renderable.
verification: automated
tests: ["tests/TaskPilot.Tests.Integration/TaskTypes/TaskTypeApiTests.cs", "tests/TaskPilot.Tests.Unit/Services/TaskTypeServiceTests.cs"]
---
An authenticated user can retrieve the list of available task types, which includes the six standard types (Task, Goal, Habit, Meeting, Note, Event), each returned with a positive id and a non-empty name.
