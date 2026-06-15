---
id: NFR-API-003
type: NFR
area: API
provenance: test
status: unratified
verification: automated
why: Echoing page/pageSize in meta lets clients paginate deterministically without guessing server behavior.
tests: ["tests/TaskPilot.Tests.Integration/Audit/AuditApiTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/TasksApiTests.cs"]
---
Collection (list) endpoints return a paged envelope carrying a `meta` block whose `page` and `pageSize` echo the requested pagination parameters.
