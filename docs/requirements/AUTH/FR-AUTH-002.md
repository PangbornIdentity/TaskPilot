---
id: FR-AUTH-002
type: FR
area: AUTH
provenance: test
status: unratified
verification: automated
why: Email is the unique account identifier; allowing a duplicate would create ambiguous accounts and let one registration silently shadow or hijack another.
tests: ["tests/TaskPilot.Tests.Integration/Auth/AuthApiTests.cs"]
---
Registering with an email that is already in use is rejected as a bad request (HTTP 400) and no duplicate account is created.
