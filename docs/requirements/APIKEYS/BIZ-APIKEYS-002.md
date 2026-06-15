---
id: BIZ-APIKEYS-002
type: BIZ
area: APIKEYS
provenance: test
status: unratified
verification: automated
why: This is the authentication gate for the REST API — only an active, hash-matching key may act on its owner's behalf, and inactive or unknown keys must be rejected.
tests: ["tests/TaskPilot.Tests.Unit/Services/ApiKeyServiceTests.cs"]
---
Key validation succeeds only for an active key whose HMAC-SHA256 hash matches a stored key, returning that key's name, owning user id, and key id; an inactive key or an unknown (no hash match) key validates as invalid with null name, user id, and key id.
