---
id: BIZ-TASKS-011
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: Defaulting to no status filter avoids hiding tasks the user did not explicitly choose to hide.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskRepositoryFilterTests.cs"]
---
When no incomplete-only or overdue-only filter is supplied, the task listing applies neither filter and returns tasks of all statuses.
