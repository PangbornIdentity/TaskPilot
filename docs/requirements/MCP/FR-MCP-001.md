---
id: FR-MCP-001
type: FR
area: MCP
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Mcp/TaskPilotMcpToolsTests.cs"]
why: list_tasks is the LLM's primary read path; rejecting an unrecognized status string surfaces a clear error instead of silently returning wrong results.
---
The MCP "list tasks" tool returns the user's tasks as a serialized paged result; an optional status filter string is parsed into the corresponding task-status enum and applied to the query, and an unrecognized status string raises an argument error.
