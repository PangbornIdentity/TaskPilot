---
id: NFR-SEC-009
type: NFR
area: SEC
provenance: doc
status: unratified
verification: automated
tests: []
why: SUGGESTED: Hardened auth-cookie attributes reduce CSRF and cookie-theft risk in production.
---
The authentication cookie is hardened with HttpOnly, SameSite=Strict, SecurePolicy=Always, a 14-day expiry, and sliding expiration.

Status: aspirational — not yet implemented (iteration 2; ARCHITECTURE §4.1).
