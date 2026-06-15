---
id: NFR-SEC-005
type: NFR
area: SEC
provenance: code
status: unratified
verification: automated
why: Header-driven scheme selection lets one app serve both cookie-auth UI and key-auth API without ambiguity.
tests: []
---
The authentication scheme is selected per request by header presence: a request carrying the `X-Api-Key` header is authenticated with the API-key scheme, and any request without it falls back to the cookie scheme.
