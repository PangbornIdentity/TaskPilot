---
id: FR-AUDIT-010
type: FR
area: AUDIT
provenance: code
status: unratified
why: Conjunctive filters let an owner narrow a large audit log to a specific key, time window, method, or status range when investigating API activity.
verification: automated
tests: []
---
GET /api/v1/audit accepts optional filters that are applied conjunctively when present: apiKeyId (exact), from/to (Timestamp >= from and <= to), httpMethod (exact match), statusCodeMin (ResponseStatusCode >= value) and statusCodeMax (ResponseStatusCode <= value); paging defaults to page 1 with a page size of 50 when unspecified.
