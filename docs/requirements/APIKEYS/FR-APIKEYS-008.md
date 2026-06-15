---
id: FR-APIKEYS-008
type: FR
area: APIKEYS
provenance: code
status: unratified
verification: automated
why: lastUsedDate lets an owner spot stale or unexpectedly active keys and decide which to revoke.
tests: []
---
A successful key validation updates that key's LastUsedDate to the current UTC time, so the lastUsedDate subsequently reflects the most recent successful use of the key.
