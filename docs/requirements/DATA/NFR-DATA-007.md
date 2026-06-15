---
id: NFR-DATA-007
type: NFR
area: DATA
provenance: code
status: unratified
verification: automated
why: Hashing the body (never storing raw) lets the audit log prove request content without retaining sensitive payloads.
tests: []
---
The API audit log records only a SHA-256 hex hash of the request body, never the raw body; an empty or absent body is recorded as an empty hash.
