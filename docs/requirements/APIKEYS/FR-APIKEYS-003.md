---
id: FR-APIKEYS-003
type: FR
area: APIKEYS
provenance: test
status: unratified
verification: automated
why: Lets an owner instantly suspend a key suspected of misuse without permanently revoking it, so legitimate access can be restored later.
tests: ["tests/TaskPilot.Tests.Integration/ApiKeys/ApiKeysApiTests.cs"]
---
POST /api/v1/apikeys/{id}/deactivate marks the key inactive (isActive=false) and POST /api/v1/apikeys/{id}/activate marks it active again (isActive=true), each returning 204.
