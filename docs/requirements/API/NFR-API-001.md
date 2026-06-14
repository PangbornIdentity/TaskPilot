---
id: NFR-API-001
type: NFR
area: API
provenance: test
status: unratified
verification: automated
why: A uniform response envelope gives API/LLM consumers one predictable shape to parse across all endpoints.
tests: ["tests/TaskPilot.Tests.Integration/Tasks/TasksApiTests.cs", "tests/TaskPilot.Tests.Integration/Auth/AuthApiTests.cs"]
---
Every successful API response is wrapped in the standard envelope with a top-level `data` property (no bare JSON payloads).
