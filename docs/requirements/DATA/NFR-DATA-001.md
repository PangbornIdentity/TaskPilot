---
id: NFR-DATA-001
type: NFR
area: DATA
provenance: test
status: unratified
verification: automated
why: Soft delete preserves history and enables recovery/audit; hard deletes are non-negotiably disallowed.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/TasksApiTests.cs"]
---
Deleting a record is soft: it sets `IsDeleted = true` and stamps `DeletedAt`; the row is never hard-deleted and is excluded from subsequent reads.
