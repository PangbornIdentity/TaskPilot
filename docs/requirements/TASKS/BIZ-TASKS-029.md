---
id: BIZ-TASKS-029
type: BIZ
area: TASKS
provenance: code
status: unratified
verification: automated
why: Computing the successor's due date from the recurrence pattern keeps recurring tasks on their intended cadence automatically.
tests: []
---
When a recurring task is completed, the successor task's target date is computed from the source task's target date by adding one day for a Daily pattern, seven days for a Weekly pattern, and one calendar month for a Monthly pattern; if the source has no target date the successor's target date is null.
