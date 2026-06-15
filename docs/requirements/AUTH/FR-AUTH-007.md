---
id: FR-AUTH-007
type: FR
area: AUTH
provenance: code
status: unratified
verification: automated
why: Requiring the current password before a change prevents a hijacked session from silently locking out the real owner; per-field errors guide the user to a compliant new password.
tests: []
---
An authenticated user can change their own password by supplying their current password and a new password; on success the change is applied and HTTP 204 (No Content) is returned, while a failed change (e.g. wrong current password or a new password that violates password policy) is rejected as a bad request (HTTP 400) with per-error field details.
