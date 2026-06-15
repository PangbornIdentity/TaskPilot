---
id: BIZ-TASKS-024
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: Cloning must be non-destructive to the source so users can copy freely without side effects on the original.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceCloneTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/CloneTaskEndpointTests.cs"]
---
Cloning leaves the source task unchanged: its status, last-modified timestamp, and activity log are not altered.
