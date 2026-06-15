---
id: FR-APIKEYS-001
type: FR
area: APIKEYS
provenance: test
status: unratified
verification: automated
why: Only the hash is stored, so the plain-text key can never be recovered later — it must be shown to the caller exactly once at creation or it is lost.
tests: ["tests/TaskPilot.Tests.Integration/ApiKeys/ApiKeysApiTests.cs", "tests/TaskPilot.Tests.Unit/Services/ApiKeyServiceTests.cs"]
---
Creating an API key (POST /api/v1/apikeys with a name) returns 201 and exposes the full plain-text key exactly once in the response; the key's prefix equals the first 8 characters of that plain-text key.
