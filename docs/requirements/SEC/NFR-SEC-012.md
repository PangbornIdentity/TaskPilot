---
id: NFR-SEC-012
type: NFR
area: SEC
provenance: human
status: ratified
verification: automated
tests: ["tests/TaskPilot.Tests.Integration/Auth/AccountLockoutTests.cs"]
why: Throttles online password-guessing / brute-force attacks.
---
After 5 consecutive failed login attempts an account is locked for 15 minutes, during which login fails even with the correct password.
