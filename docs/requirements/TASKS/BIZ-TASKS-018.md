---
id: BIZ-TASKS-018
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: Honoring an explicit title override lets users name a clone meaningfully at creation time.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceCloneTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/CloneTaskEndpointTests.cs"]
---
When a non-blank title override is supplied, the cloned task uses that title.
