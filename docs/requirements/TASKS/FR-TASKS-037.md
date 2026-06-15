---
id: FR-TASKS-037
type: FR
area: TASKS
provenance: doc
status: ratified
verification: automated
why: SUGGESTED: Keyboard shortcuts speed up power-user task workflows without reaching for the mouse.
tests: []
---
The application provides global keyboard shortcuts for task workflows: `N` opens the new-task create panel, `E` edits the selected task, `/` focuses the search input, `Esc` closes the active slide-over or modal, `?` shows a keyboard-shortcuts overlay, and `Space` toggles the selected task complete.

Status: aspirational — not yet implemented. No keyboard-shortcut handling exists in the application JavaScript or Razor pages (only vendor libraries are present under wwwroot, and the inline Tasks page script handles clone, filter persistence, and inline tag creation only).
