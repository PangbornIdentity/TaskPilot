---
id: FR-TASKS-039
type: FR
area: TASKS
provenance: doc
status: unratified
verification: automated
why: SUGGESTED: Auto-saving a draft protects users from losing in-progress input if they navigate away mid-edit.
tests: []
---
The task create/edit form auto-saves an in-progress draft to `localStorage` if the user navigates away mid-edit, so the unsaved input can be restored.

Status: aspirational — not yet implemented. No `localStorage` draft persistence exists in the Tasks page or application JavaScript (the only client-side persistence is sessionStorage filter state and clone toast handoff).
