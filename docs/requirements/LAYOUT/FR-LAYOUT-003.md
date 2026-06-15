---
id: FR-LAYOUT-003
type: FR
area: LAYOUT
provenance: code
status: unratified
verification: automated
tests: []
why: Normalizing sort inputs (and defaulting to priority asc) gives a predictable, abuse-tolerant ordering while keeping header chevron state accurate to the URL.
---
When no sort column is supplied in the URL, the Tasks page leaves SortBy/SortDir null for header-state purposes but passes the repository default sort of "priority" ascending; an explicit sortDir is normalized to "desc" only when equal to "desc" (case-insensitive), otherwise "asc", and sortBy is lower-cased.
