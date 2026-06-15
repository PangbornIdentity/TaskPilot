---
id: FR-AUTH-005
type: FR
area: AUTH
provenance: test
status: unratified
verification: automated
why: Logout must reliably terminate the session so a shared or unattended device cannot retain owner access after the user intends to leave.
tests: ["tests/TaskPilot.Tests.E2E/Auth/AuthTests.cs", "tests/TaskPilot.Tests.Integration/Auth/AuthApiTests.cs"]
---
An authenticated user can log out, which clears their session (HTTP 204 on the API); on the web UI logging out redirects the user to the login page.
