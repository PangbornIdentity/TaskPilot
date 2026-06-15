---
id: FR-LAYOUT-006
type: FR
area: LAYOUT
provenance: code
status: unratified
verification: automated
tests: []
why: Reporting the post-filter count means the displayed total matches what the user actually sees, not a larger pre-filter number that would look wrong.
---
The Tasks page reports TotalCount as the number of tasks shown after the page-layer post-filter for the current scope (i.e. the size of the rendered list), not the repository's unpaged total.
