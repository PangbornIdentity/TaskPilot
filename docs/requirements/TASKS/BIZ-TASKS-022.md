---
id: BIZ-TASKS-022
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: A clone is appended at the end of the ordering for the same reason as any new task: to avoid disturbing existing manual positions.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceCloneTests.cs"]
---
A cloned task receives a sort order of one greater than the user's current maximum task sort order (one when no other tasks exist).
