---
id: NFR-API-006
type: NFR
area: API
provenance: code
status: unratified
verification: automated
why: Distinct 404/400/409 codes let consumers distinguish missing vs. invalid vs. conflicting requests unambiguously.
tests: []
---
A request for a resource that does not exist (or is not visible to the caller) returns HTTP 404 with error code `NOT_FOUND`, distinct from validation (`VALIDATION_ERROR` / 400) and conflict (`CONFLICT` / 409) error codes.
