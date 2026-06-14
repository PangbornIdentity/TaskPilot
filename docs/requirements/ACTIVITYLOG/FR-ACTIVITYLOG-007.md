---
id: FR-ACTIVITYLOG-007
type: FR
area: ACTIVITYLOG
provenance: code
status: unratified
why: Most-recent-first ordering surfaces the latest changes first, and resolving taskTitle at query time (even for soft-deleted tasks) keeps each entry readable rather than showing a bare ID.
verification: automated
tests: []
---
GET /api/v1/activity-logs returns entries ordered by Timestamp descending (most recent first), and each entry's taskTitle is resolved from the owning task's current title at query time (including soft-deleted tasks).
