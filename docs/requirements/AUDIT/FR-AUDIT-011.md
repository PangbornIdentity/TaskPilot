---
id: FR-AUDIT-011
type: FR
area: AUDIT
provenance: code
status: unratified
why: SUGGESTED: These fields are what the audit table renders per row, and requestBodyHash records what was sent without ever storing the full body.
verification: automated
tests: []
---
Each audit access-log entry exposes id, apiKeyId, apiKeyName, timestamp, httpMethod, endpoint, requestBodyHash, responseStatusCode, and durationMs.
