---
id: FR-TASKS-033
type: FR
area: TASKS
provenance: code
status: unratified
verification: automated
why: Requiring a date for SpecificDay prevents an inconsistent task that claims a specific day with no date.
tests: []
---
Creating or fully updating a task is rejected when the target-date type is SpecificDay but no target date is supplied.
