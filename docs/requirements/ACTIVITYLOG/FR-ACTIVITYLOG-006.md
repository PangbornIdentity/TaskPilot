---
id: FR-ACTIVITYLOG-006
type: FR
area: ACTIVITYLOG
provenance: code
status: unratified
why: Conjunctive filters let an owner narrow change history to a task, time window, field, or actor when reviewing what changed.
verification: automated
tests: []
---
GET /api/v1/activity-logs accepts optional filters applied conjunctively when present: taskId (exact), from/to (Timestamp >= from and <= to), fieldChanged (exact match), and changedBy (case-sensitive substring match); paging defaults to page 1 with a page size of 50 when unspecified.
