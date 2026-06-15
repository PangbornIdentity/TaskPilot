---
id: FR-LAYOUT-005
type: FR
area: LAYOUT
provenance: code
status: unratified
verification: automated
tests: []
why: Knowing when any filter/sort deviates from the default lets the page show a "Reset filters" affordance so users can escape a narrowed view in one click.
---
The Tasks page treats filters as active (driving the "Reset filters" affordance) when the show scope is not "active", or any of status / priority / area / task-type / tag / overdue filters is set, or an explicit sort column is present.
