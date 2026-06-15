---
id: FR-SETTINGS-009
type: FR
area: SETTINGS
provenance: code
status: unratified
verification: automated
tests: []
why: A tag that no longer exists (or isn't the user's) can't have an edit row, so the flow aborts cleanly with a message instead of rendering a stale form.
---
Updating a tag that resolves to no record (not found for the user) aborts with a "Tag not found" error message via redirect rather than re-rendering the inline edit row.
