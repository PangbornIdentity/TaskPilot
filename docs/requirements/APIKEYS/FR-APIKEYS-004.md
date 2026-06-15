---
id: FR-APIKEYS-004
type: FR
area: APIKEYS
provenance: human
status: ratified
verification: automated
why: Permanent revocation must remove the key from the owner's management view so a compromised credential cannot be reactivated or mistaken for a live key.
tests: ["tests/TaskPilot.Tests.Integration/ApiKeys/ApiKeysApiTests.cs", "tests/TaskPilot.Tests.Unit/Services/ApiKeySoftDeleteTests.cs", "tests/TaskPilot.Tests.Integration/ApiKeys/ApiKeyRevokeRejectionTests.cs"]
---
Revoking an API key soft-deletes it (IsDeleted + DeletedAt, IsActive=false); it no longer appears in the key list and can no longer authenticate a request.
