---
id: FR-MCP-006
type: FR
area: MCP
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Mcp/TaskPilotMcpToolsTests.cs"]
why: A distinct success vs invalid-operation result lets the LLM confirm a delete happened rather than assuming it did.
---
The MCP "delete task" tool returns a success result when the task is deleted, and raises an invalid-operation error when the task does not exist.
