---
id: BIZ-TASKS-023
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: Recording the clone's origin in its activity log preserves provenance so users can trace where a copied task came from.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceCloneTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/CloneTaskEndpointTests.cs"]
---
Cloning writes exactly one activity-log entry on the new task with field "Created", a null old value, and a new value of "Cloned from {sourceId}".
