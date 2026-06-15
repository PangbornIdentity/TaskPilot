---
id: BIZ-TASKS-009
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: Surfacing overdue work helps users catch commitments they have missed; cancelled and completed tasks are not actionable so are excluded.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskRepositoryFilterTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/TasksApiTests.cs"]
---
The overdue-only filter returns exactly the tasks that have a non-null target date in the past and an incomplete status, excluding Completed and Cancelled tasks.
