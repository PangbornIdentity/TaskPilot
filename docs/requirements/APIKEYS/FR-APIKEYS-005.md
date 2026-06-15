---
id: FR-APIKEYS-005
type: FR
area: APIKEYS
provenance: human
status: ratified
verification: automated
why: SUGGESTED: Lets owners relabel a key to track which LLM client uses it without rotating the credential; renaming a missing key must 404 rather than silently no-op.
tests: ["tests/TaskPilot.Tests.Integration/ApiKeys/RenameApiKeyValidationTests.cs"]
---
Renaming an API key (PATCH /api/v1/apikeys/{id}/rename) validates the new name (required, ≤100 chars) and returns 400 on violation; a valid rename returns 204; a missing/cross-user key returns 404.
