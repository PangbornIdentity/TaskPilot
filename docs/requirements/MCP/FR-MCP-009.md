---
id: FR-MCP-009
type: FR
area: MCP
provenance: code
status: unratified
verification: automated
tests: []
why: Capping page size at 100 protects the server from an LLM requesting an unbounded page that could exhaust memory or time out.
---
The MCP "list_tasks" tool caps the effective page size at 100 (clamping any larger requested pageSize down to 100) before querying.
