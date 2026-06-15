---
id: FR-MCP-002
type: FR
area: MCP
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Mcp/TaskPilotMcpToolsTests.cs"]
why: An explicit error for an unknown id lets the calling LLM distinguish "no such task" from a transient failure and recover.
---
The MCP "get task" tool returns the requested task serialized when it exists, and raises an invalid-operation error when no task matches the id.
