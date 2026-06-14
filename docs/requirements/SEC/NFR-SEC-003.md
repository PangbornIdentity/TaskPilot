---
id: NFR-SEC-003
type: NFR
area: SEC
provenance: test
status: unratified
verification: automated
why: Returning 404 (not 403) for cross-owner access prevents resource-existence enumeration and enforces owner-scoped isolation.
tests: ["tests/TaskPilot.Tests.Integration/Tasks/TasksApiTests.cs", "tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs"]
---
A request to access another user's resource by id returns HTTP 404 (not 403): data access is scoped to the authenticated owner so cross-owner records are indistinguishable from non-existent ones.
