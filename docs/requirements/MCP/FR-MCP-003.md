---
id: FR-MCP-003
type: FR
area: MCP
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Mcp/TaskPilotMcpToolsTests.cs"]
why: create_task is the core write capability that lets an LLM add work on the user's behalf; returning the serialized task confirms what was created.
---
The MCP "create task" tool creates a task for the calling user from the supplied fields and returns the serialized task.
