---
id: NFR-SEC-002
type: NFR
area: SEC
provenance: test
status: unratified
verification: automated
why: Rejecting missing/invalid/inactive keys with 401 is the front door of API-key auth; without it the REST surface is open.
tests: ["tests/TaskPilot.Tests.Integration/Auth/AuthApiTests.cs"]
---
An API request whose `X-Api-Key` header is missing, invalid, or belongs to an inactive key is rejected with HTTP 401 Unauthorized.
