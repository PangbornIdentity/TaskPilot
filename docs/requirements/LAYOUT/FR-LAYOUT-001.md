---
id: FR-LAYOUT-001
type: FR
area: LAYOUT
provenance: code
status: unratified
verification: automated
tests: []
why: Defaulting an omitted or garbage scope/view to safe values ("active"/"list") means a hand-edited or stale URL still renders a sensible Tasks page.
---
The Tasks page "show" scope accepts "active", "completed", or "all"; an omitted or unrecognized value falls back to "active" (NotStarted, InProgress, Blocked), and the "view" parameter falls back to "list" for anything other than "board".
