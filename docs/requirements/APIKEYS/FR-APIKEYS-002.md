---
id: FR-APIKEYS-002
type: FR
area: APIKEYS
provenance: test
status: unratified
verification: automated
why: Exposing the plain-text key or stored hash on a list endpoint would defeat the show-once secret model and leak credentials usable against the REST API.
tests: ["tests/TaskPilot.Tests.Integration/ApiKeys/ApiKeysApiTests.cs"]
---
GET /api/v1/apikeys returns all of the user's API keys, and key listing responses never expose the plain-text key or the stored key hash.
