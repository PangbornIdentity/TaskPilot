---
id: BIZ-TASKS-034
type: BIZ
area: TASKS
provenance: code
status: unratified
verification: automated
why: Treating an empty user as max-order zero makes the first task's sort order deterministic (one) rather than undefined.
tests: []
---
The user's maximum task sort order is treated as zero when the user has no tasks, so the first task created for a user receives sort order one.
