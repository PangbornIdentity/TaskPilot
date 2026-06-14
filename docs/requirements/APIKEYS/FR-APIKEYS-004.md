---
id: FR-APIKEYS-004
type: FR
area: APIKEYS
provenance: test
status: unratified
verification: automated
why: Permanent revocation must remove the key from the owner's management view so a compromised credential cannot be reactivated or mistaken for a live key.
tests: ["tests/TaskPilot.Tests.Integration/ApiKeys/ApiKeysApiTests.cs"]
---
DELETE /api/v1/apikeys/{id} revokes the key (returns 204), after which it no longer appears in the user's key list.
