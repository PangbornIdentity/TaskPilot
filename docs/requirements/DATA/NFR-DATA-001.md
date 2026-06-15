---
id: NFR-DATA-001
type: NFR
area: DATA
provenance: human
status: ratified
verification: automated
why: Soft delete preserves history and enables recovery/audit; hard deletes are non-negotiably disallowed.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/TasksApiTests.cs", "tests/TaskPilot.Tests.Unit/Services/TagSoftDeleteTests.cs", "tests/TaskPilot.Tests.Unit/Services/ApiKeySoftDeleteTests.cs", "tests/TaskPilot.Tests.Integration/ApiKeys/ApiKeyRevokeRejectionTests.cs"]
---
Deletes are soft for every deletable entity (TaskItem, Tag, ApiKey): IsDeleted is set true with a DeletedAt timestamp, the row is never physically removed, and it is excluded from reads by a global query filter.
