---
id: FR-AUTH-006
type: FR
area: AUTH
provenance: test
status: unratified
verification: automated
why: A "me" endpoint lets the UI and API clients confirm who the current session belongs to without exposing other users' data.
tests: ["tests/TaskPilot.Tests.Integration/Auth/AuthApiTests.cs"]
---
An authenticated user can retrieve their own current account details (id and email) via the "me" endpoint.
