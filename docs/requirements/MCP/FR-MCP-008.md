---
id: FR-MCP-008
type: FR
area: MCP
provenance: code
status: unratified
verification: automated
tests: []
why: Clone lets the LLM spin up a near-duplicate task (e.g. a recurring chore) without re-specifying every field, with a sensible "(copy)" title default.
---
The MCP "clone_task" tool duplicates an existing task for the calling user (defaulting the clone title to "<source title> (copy)" when no title override is given, honouring a target-date override or a clear-target-date flag) and raises an invalid-operation error when the source task does not exist or cannot be cloned.
