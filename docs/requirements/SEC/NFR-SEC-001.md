---
id: NFR-SEC-001
type: NFR
area: SEC
provenance: test
status: unratified
verification: automated
why: Unauthenticated users must never reach protected pages; redirecting to login enforces the cookie-auth boundary.
tests: ["tests/TaskPilot.Tests.E2E/Auth/AuthTests.cs"]
---
An unauthenticated browser request to an authenticated page is redirected to the login page (`/auth/login`).
