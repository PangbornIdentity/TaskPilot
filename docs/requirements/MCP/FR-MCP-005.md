---
id: FR-MCP-005
type: FR
area: MCP
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Mcp/TaskPilotMcpToolsTests.cs"]
why: Letting the LLM mark tasks complete with an optional result note captures outcomes at the moment work finishes.
---
The MCP "complete task" tool marks the task complete (forwarding an optional result-analysis note) and returns the serialized task.
