---
id: BIZ-TASKS-039
type: BIZ
area: TASKS
provenance: code
status: unratified
verification: automated
why: A stable sort-order tie-breaker makes paginated results deterministic so rows do not shuffle or duplicate across pages when the primary key ties.
tests: []
---
Every explicit column-header sort of the task listing (title, area, type, status, target date, created date, last-modified date, priority) appends the task sort order as a final tie-breaker so that pagination order is deterministic when the primary sort key ties.
