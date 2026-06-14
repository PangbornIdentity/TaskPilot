---
id: BIZ-TASKS-012
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: Priority-then-due default ordering surfaces the most important and most time-sensitive work first; nulls-last keeps undated tasks from crowding the top.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskRepositoryFilterTests.cs"]
---
The default sort order for the task listing is priority descending, then target date ascending with null target dates ordered last.
