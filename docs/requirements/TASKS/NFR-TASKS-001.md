---
id: NFR-TASKS-001
type: NFR
area: TASKS
provenance: test
status: unratified
verification: automated
why: Atomic cloning prevents orphaned or partial rows that would corrupt the task list and its activity log if a clone half-fails.
tests: ["tests/TaskPilot.Tests.Integration/Tasks/CloneTaskEndpointTests.cs"]
---
Cloning a task is atomic: if the persistence operation fails, no partial rows (neither the new task nor its activity log) are committed.
