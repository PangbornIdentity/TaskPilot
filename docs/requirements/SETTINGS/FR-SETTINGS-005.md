---
id: FR-SETTINGS-005
type: FR
area: SETTINGS
provenance: code
status: unratified
verification: automated
tests: []
why: Ignoring blank key names avoids creating unlabelled keys the user can't later tell apart in the keys table.
---
On the Settings page, generating an API key with a null or whitespace-only name is ignored (redirect without creating a key); a valid name is trimmed before the key is created.
