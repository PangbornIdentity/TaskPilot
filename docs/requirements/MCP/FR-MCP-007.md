---
id: FR-MCP-007
type: FR
area: MCP
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Mcp/TaskPilotMcpToolsTests.cs"]
why: Exposing stats, tags, and task-types as read tools gives the LLM the context it needs to create and filter tasks with valid values.
---
The MCP toolset exposes read tools that return serialized stats, the user's tag list, and the active task-type list.
