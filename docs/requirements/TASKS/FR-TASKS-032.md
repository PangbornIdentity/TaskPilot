---
id: FR-TASKS-032
type: FR
area: TASKS
provenance: code
status: unratified
verification: automated
why: Title and task-type validation prevents creating malformed or unidentifiable tasks.
tests: []
---
Creating or fully updating a task is rejected when the title is empty, when the title exceeds 200 characters, or when the task type id is not greater than zero.
