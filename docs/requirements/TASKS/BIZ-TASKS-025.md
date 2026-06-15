---
id: BIZ-TASKS-025
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: Returning not-found for inaccessible sources enforces ownership isolation and avoids leaking the existence of other users' or deleted tasks.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceCloneTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/CloneTaskEndpointTests.cs"]
---
Cloning a non-existent, soft-deleted, or another user's task yields no clone (returning not-found at the API), and missing and soft-deleted sources produce the same not-found response.
