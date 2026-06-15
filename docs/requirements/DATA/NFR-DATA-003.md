---
id: NFR-DATA-003
type: NFR
area: DATA
provenance: test
status: unratified
verification: automated
why: SUGGESTED: API traffic must be attributable and auditable per-key for accountability and incident review.
tests: []
---
Every non-health `/api/v1` request authenticated via `X-Api-Key` produces exactly one `ApiAuditLog` row whose columns capture the HTTP method, endpoint path, response status code, duration (ms), the authenticating API key's id and name, the resolved user id, and the request body hash (`BodyHash`).
