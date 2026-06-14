---
id: FR-APIKEYS-007
type: FR
area: APIKEYS
provenance: code
status: unratified
verification: automated
why: A required, length-bounded name keeps the key list human-readable and prevents unlabeled or oversized labels from being persisted.
tests: []
---
Creating an API key requires a non-empty name of at most 100 characters; a missing/blank name or a name exceeding 100 characters is rejected with a validation error (400) and the key is not created.
