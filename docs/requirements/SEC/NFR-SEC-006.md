---
id: NFR-SEC-006
type: NFR
area: SEC
provenance: code
status: unratified
verification: automated
why: Deny-by-default authorization with an explicit anonymous allow-list prevents accidentally exposing new pages.
tests: []
---
All Razor Pages require authentication by default (the entire `/` folder is authorized); only an explicit allow-list (Login, Register, Error, Health) is reachable anonymously.
