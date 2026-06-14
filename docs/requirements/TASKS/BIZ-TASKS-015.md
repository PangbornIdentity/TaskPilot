---
id: BIZ-TASKS-015
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: A clone should reproduce the source's defining attributes so it is a faithful starting copy, while a new id keeps it independent.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceCloneTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/CloneTaskEndpointTests.cs"]
---
A cloned task copies the source's description, task type, area, priority, target-date type, and recurrence settings verbatim while receiving a new distinct id.
