---
id: BIZ-TASKS-013
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: Letting users sort by any key column makes large task lists navigable to the user's current need.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskRepositoryFilterTests.cs"]
---
The task listing can be sorted by title, status, area, or task-type name in either ascending or descending direction; status and area sort by their underlying enum order and type sorts by task-type name.
