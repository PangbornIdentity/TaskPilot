---
id: NFR-SEC-007
type: NFR
area: SEC
provenance: code
status: unratified
verification: automated
why: Deriving actor provenance server-side prevents clients from forging audit attribution in LastModifiedBy.
tests: []
---
The actor-provenance string (`LastModifiedBy`) is derived server-side from the authenticated principal's claims — `api:{keyName}` when the request authenticated via the API-key scheme, otherwise `user:{username}` — and is never taken from client-supplied input.
