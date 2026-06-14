---
id: FR-TASKS-036
type: FR
area: TASKS
provenance: code
status: unratified
verification: automated
why: Explicit reorder lets the ordering be set programmatically while ownership checks prevent reordering another user's tasks.
tests: []
---
A user can reorder a task by setting its sort order to an explicit value; the operation succeeds only for a task owned by the user and otherwise reports failure.
