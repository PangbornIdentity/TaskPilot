---
id: FR-TASKS-034
type: FR
area: TASKS
provenance: code
status: unratified
verification: automated
why: Requiring a valid recurrence pattern when recurring prevents recurring tasks that cannot compute a successor.
tests: []
---
Creating or fully updating a task is rejected when the task is flagged recurring but no recurrence pattern is supplied, or when the supplied recurrence pattern is not a defined pattern value.
