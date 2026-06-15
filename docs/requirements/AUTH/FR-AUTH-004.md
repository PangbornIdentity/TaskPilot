---
id: FR-AUTH-004
type: FR
area: AUTH
provenance: test
status: unratified
verification: automated
why: Rejecting bad credentials without authenticating is the core protection against unauthorized access; the 401 and on-page error give the user clear feedback without leaking which factor was wrong.
tests: ["tests/TaskPilot.Tests.E2E/Auth/AuthTests.cs", "tests/TaskPilot.Tests.Integration/Auth/AuthApiTests.cs"]
---
A login attempt with an incorrect password is rejected (HTTP 401) and is not authenticated; on the web UI the user remains on the login page and is shown an invalid-credentials error message.
