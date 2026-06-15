---
id: BIZ-TASKS-020
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: Clone target-date override semantics let users either reuse, change, or clear the due date so the copy fits its new purpose.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceCloneTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/CloneTaskEndpointTests.cs"]
---
A cloned task copies the source target date verbatim when no override is given; a supplied target-date override replaces it; and a clear-target-date flag forces the clone's target date to null, taking precedence over any supplied override.
