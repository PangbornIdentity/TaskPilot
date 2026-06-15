---
id: BIZ-TASKS-041
type: BIZ
area: TASKS
provenance: code
status: unratified
verification: automated
why: SUGGESTED: The conditional default sort applies the priority-first ordering only for the focused incomplete view, with a predictable fallback elsewhere to avoid surprising orderings.
tests: []
---
The priority-first default sort (priority ascending, then target date ascending nulls-last, then sort order) is applied only when the incomplete-only filter is active and no explicit sort column is requested; otherwise an unrecognized or default sort key falls back to sorting by priority alone (ascending or descending) with a sort-order tie-breaker.
