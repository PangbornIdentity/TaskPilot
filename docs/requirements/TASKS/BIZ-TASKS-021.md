---
id: BIZ-TASKS-021
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: Copying the source's tags preserves categorization so a clone is filed the same way as its original by default.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceCloneTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/CloneTaskEndpointTests.cs"]
---
A cloned task copies all of the source task's tags; if the source has no tags the clone has none.
