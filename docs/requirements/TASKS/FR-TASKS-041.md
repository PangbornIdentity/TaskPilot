---
id: FR-TASKS-041
type: FR
area: TASKS
provenance: doc
status: unratified
verification: automated
why: SUGGESTED: Drag-and-drop reordering gives users an intuitive way to set manual task priority order.
tests: []
---
A user can reorder tasks within a view by drag-and-drop, persisting the new manual `SortOrder`. (Distinct from FR-TASKS-036, which covers the service-level `UpdateSortOrderAsync` method: this record covers the user-facing drag-and-drop interaction and the HTTP/UI surface that drives it.)

Status: aspirational — not yet implemented. `ITaskService.UpdateSortOrderAsync` exists but is never invoked by any controller action or Razor page handler, and no drag-and-drop UI is present in the Tasks page. There is no HTTP endpoint exposing reorder.
