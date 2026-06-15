---
id: NFR-DATA-004
type: NFR
area: DATA
provenance: test
status: unratified
verification: automated
why: An immutable per-field activity log gives a complete change history (what/old/new/who) for each task.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs"]
---
Mutating a task appends an immutable per-field activity-log entry capturing the field changed, old value, new value, and the actor (`ChangedBy`).
