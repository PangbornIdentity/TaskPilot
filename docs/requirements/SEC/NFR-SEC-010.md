---
id: NFR-SEC-010
type: NFR
area: SEC
provenance: human
status: ratified
verification: automated
tests: ["tests/TaskPilot.Tests.Integration/ApiKeys/ApiKeySchemeRestrictionTests.cs"]
why: Prevents an API key (a lower-trust credential held by an LLM/automation) from escalating to manage the credential set itself.
---
The API-key management endpoints under /api/v1/apikeys require cookie (session) authentication; a request authenticated only with X-Api-Key is rejected with 401, so an API key cannot list, create, rename, activate, deactivate, or revoke API keys.
