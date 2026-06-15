---
id: NFR-API-002
type: NFR
area: API
provenance: test
status: unratified
verification: automated
why: Rejecting invalid input with 400 keeps malformed data out of the service/persistence layers.
tests: ["tests/TaskPilot.Tests.Integration/Tasks/TasksApiTests.cs"]
---
A request that fails input validation is rejected with HTTP 400 Bad Request rather than being processed.
