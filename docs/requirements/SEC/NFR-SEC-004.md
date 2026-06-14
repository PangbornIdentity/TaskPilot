---
id: NFR-SEC-004
type: NFR
area: SEC
provenance: test
status: unratified
verification: automated
why: Storing only the HMAC-SHA256 hash means a database leak cannot expose usable API keys.
tests: ["tests/TaskPilot.Tests.Unit/Services/ApiKeyServiceTests.cs", "tests/TaskPilot.Tests.Integration/ApiKeys/ApiKeysApiTests.cs"]
---
API keys are persisted only as an HMAC-SHA256 hash (never as plain text); the plain-text key is returned to the caller exactly once at generation and is never readable from any subsequent response or store.
