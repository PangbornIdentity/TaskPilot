---
id: FR-AUTH-008
type: FR
area: AUTH
provenance: code
status: unratified
verification: automated
why: Honoring returnUrl restores the user's intended destination after login, while restricting it to local redirects blocks open-redirect attacks that could send the user to a malicious off-site URL.
tests: []
---
On a successful web-UI login, the user is redirected to the supplied returnUrl when one is present, falling back to the site root ("/") otherwise. The redirect is performed via LocalRedirect, so an off-site (non-local) returnUrl is rejected rather than followed — LocalRedirect throws (yielding an HTTP 500) rather than silently redirecting to the site root, which prevents open-redirect attacks.
