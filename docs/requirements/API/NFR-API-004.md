---
id: NFR-API-004
type: NFR
area: API
provenance: code
status: unratified
verification: automated
why: Centralized 500 handling with a generic message prevents leaking stack traces or internals to callers.
tests: []
---
Any unhandled exception escaping the request pipeline is caught centrally and returned as an HTTP 500 response carrying the standard error envelope with code `INTERNAL_ERROR` and a generic message; the exception detail is logged but never leaked to the caller.
