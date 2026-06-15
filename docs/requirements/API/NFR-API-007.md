---
id: NFR-API-007
type: NFR
area: API
provenance: code
status: unratified
verification: automated
why: A per-response requestId correlates client reports with server logs for tracing and support.
tests: []
---
Every successful response envelope carries a `meta.requestId` correlation identifier; when the caller does not supply one a fresh GUID is generated per response.
