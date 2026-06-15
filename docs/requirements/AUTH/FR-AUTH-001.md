---
id: FR-AUTH-001
type: FR
area: AUTH
provenance: test
status: unratified
verification: automated
why: Registration is the entry point for the owner to obtain an account; auto sign-in plus dashboard redirect removes a friction step so a new user lands in a usable state immediately.
tests: ["tests/TaskPilot.Tests.E2E/Auth/AuthTests.cs", "tests/TaskPilot.Tests.Integration/Auth/AuthApiTests.cs"]
---
A new user can register with an email and password, which creates an account and returns the new user's id; on the web UI a successful registration signs the user in and redirects them to the dashboard.
