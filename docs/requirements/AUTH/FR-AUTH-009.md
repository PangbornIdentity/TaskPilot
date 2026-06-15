---
id: FR-AUTH-009
type: FR
area: AUTH
provenance: code
status: unratified
verification: automated
why: Restricting logout to POST prevents CSRF-style forced logout via a planted GET link or prefetched image, so a third party cannot sign the owner out unexpectedly.
tests: []
---
Web-UI logout is performed only via POST; issuing a GET to the logout page does not sign the user out and instead redirects to the site root ("/").
