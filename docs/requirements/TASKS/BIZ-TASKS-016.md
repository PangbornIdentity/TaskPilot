---
id: BIZ-TASKS-016
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: A clone represents fresh work, so it must start un-started with no carried-over completion state or stale analysis.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceCloneTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/CloneTaskEndpointTests.cs"]
---
A cloned task is always created with status NotStarted and with a null completion timestamp and null result analysis, regardless of the source task's status.
