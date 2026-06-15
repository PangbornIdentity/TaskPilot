---
id: FR-MCP-010
type: FR
area: MCP
provenance: code
status: unratified
verification: automated
tests: []
why: Case-insensitive parsing plus an error that lists valid values lets an LLM self-correct after passing a wrong enum, raising tool-call success rates.
---
MCP enum-valued parameters are parsed case-insensitively; an unrecognized value raises an argument error whose message enumerates the valid values, and for optional filter parameters a null/omitted value is treated as "no filter" while required create parameters reject a null/invalid value.
