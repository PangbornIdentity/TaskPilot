---
id: BIZ-APIKEYS-003
type: BIZ
area: APIKEYS
provenance: code
status: unratified
why: 32 bytes of CSPRNG entropy makes keys infeasible to guess, and a URL-safe encoding lets the key be passed in headers without escaping.
verification: automated
tests: []
---
A newly generated plain-text key is derived from 32 cryptographically random bytes encoded as URL-safe Base64 (standard Base64 with '+' replaced by '-', '/' replaced by '_', and all '=' padding removed); the stored keyPrefix is the first 8 characters of this plain-text key.
