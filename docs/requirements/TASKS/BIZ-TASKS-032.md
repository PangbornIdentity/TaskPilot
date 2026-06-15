---
id: BIZ-TASKS-032
type: BIZ
area: TASKS
provenance: code
status: unratified
verification: automated
why: Requiring both the recurring flag and a pattern prevents creating successors for tasks that were never meant to recur.
tests: []
---
A successor task is created only when the completed task is both flagged recurring and has a non-null recurrence pattern; completing a task that is recurring but has no recurrence pattern creates no successor.
