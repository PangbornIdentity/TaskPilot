---
id: FR-AUDIT-009
type: FR
area: AUDIT
provenance: code
status: unratified
why: Most-recent-first ordering puts the activity an owner most likely wants to review at the top of the audit log.
verification: automated
tests: []
---
GET /api/v1/audit returns the user's API access logs ordered by Timestamp descending (most recent first).
