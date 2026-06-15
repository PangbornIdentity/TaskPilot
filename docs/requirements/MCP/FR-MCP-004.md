---
id: FR-MCP-004
type: FR
area: MCP
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Mcp/TaskPilotMcpToolsTests.cs"]
why: Failing loudly on a missing target prevents an LLM update from silently no-op'ing against a task that doesn't exist or isn't the user's.
---
The MCP "update task" tool raises an invalid-operation error when the target task does not exist.
