---
id: BIZ-TASKS-014
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: Keeping undated tasks last under any direction prevents missing dates from masquerading as earliest or latest, which would mislead the user.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskRepositoryFilterTests.cs"]
---
When sorting by target date, tasks with no target date always sort last regardless of the sort direction.
