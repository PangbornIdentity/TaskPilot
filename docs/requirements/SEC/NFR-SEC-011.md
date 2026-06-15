---
id: NFR-SEC-011
type: NFR
area: SEC
provenance: human
status: ratified
verification: automated
tests: ["tests/TaskPilot.Tests.Integration/Auth/PasswordPolicyTests.cs"]
why: Stronger credentials reduce account-takeover risk.
---
User passwords must be at least 10 characters and include an uppercase letter, a lowercase letter, a digit, and a non-alphanumeric character; a registration with a weaker password is rejected with 400.
