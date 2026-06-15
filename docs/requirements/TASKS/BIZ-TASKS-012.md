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
The task listing has two default sort orders depending on the active view. For the incomplete-only default view (when no explicit sort column is chosen), tasks are ordered by priority ascending (Critical first, since the Priority enum is 1-based Critical=1..Low=4), then target date ascending with null target dates ordered last, then SortOrder as a stable tie-breaker. For the all-status default view, tasks are ordered by priority ascending then SortOrder, with no target-date term.
