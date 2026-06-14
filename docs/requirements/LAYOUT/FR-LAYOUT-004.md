---
id: FR-LAYOUT-004
type: FR
area: LAYOUT
provenance: code
status: unratified
verification: automated
tests: []
why: Showing only the columns relevant to the current scope keeps the kanban focused (open work vs done) instead of cluttering it with empty states.
---
The Tasks board (kanban) view renders three columns (Not Started, In Progress, Blocked) for the "active" scope, two columns (Completed, Cancelled) for "completed", and all five columns for "all", each populated by filtering the fetched tasks by status.
