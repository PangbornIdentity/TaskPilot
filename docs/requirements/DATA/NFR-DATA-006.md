---
id: NFR-DATA-006
type: NFR
area: DATA
provenance: code
status: unratified
verification: automated
why: Non-blocking, failure-swallowing audit writes ensure logging never degrades or fails the caller's request.
tests: []
---
Writing the API audit log is non-blocking with respect to the request: the audit row is persisted after the downstream response has been produced, and a failure to write the audit entry is logged and swallowed without altering the response returned to the caller.
