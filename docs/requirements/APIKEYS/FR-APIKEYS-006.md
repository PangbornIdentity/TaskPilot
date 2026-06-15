---
id: FR-APIKEYS-006
type: FR
area: APIKEYS
provenance: code
status: unratified
verification: automated
why: SUGGESTED: Alphabetical ordering and a stable field set give the keys-management table a predictable layout for owners scanning many keys.
tests: []
---
GET /api/v1/apikeys returns the user's keys ordered alphabetically by name (ascending), and each listed key exposes id, name, keyPrefix, createdDate, lastUsedDate, and isActive.
