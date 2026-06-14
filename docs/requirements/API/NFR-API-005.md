---
id: NFR-API-005
type: NFR
area: API
provenance: code
status: unratified
verification: automated
why: A consistent error envelope with machine-readable codes lets consumers handle failures programmatically.
tests: []
---
Every error response is wrapped in the standard error envelope carrying a single top-level `error` object with a machine-readable `code` and a human-readable `message` (and optional per-field `details`); no error is returned as bare JSON.
