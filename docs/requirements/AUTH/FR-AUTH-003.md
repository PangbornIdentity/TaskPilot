---
id: FR-AUTH-003
type: FR
area: AUTH
provenance: test
status: unratified
verification: automated
why: Cookie-based login is the gate for all owner web access; setting the auth cookie and redirecting off the login page is what establishes and confirms the authenticated session.
tests: ["tests/TaskPilot.Tests.E2E/Auth/AuthTests.cs", "tests/TaskPilot.Tests.Integration/Auth/AuthApiTests.cs"]
---
A user who logs in with valid credentials is authenticated (an auth cookie is set), the response returns their user details, and on the web UI they are redirected away from the login page to the dashboard.
