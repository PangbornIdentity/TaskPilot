---
id: FR-APIKEYS-005
type: FR
area: APIKEYS
provenance: code
status: unratified
verification: automated
why: SUGGESTED: Lets owners relabel a key to track which LLM client uses it without rotating the credential; renaming a missing key must 404 rather than silently no-op.
tests: []
---
PATCH /api/v1/apikeys/{id}/rename sets the key's name to the supplied value and returns 204; renaming a non-existent key returns 404.
