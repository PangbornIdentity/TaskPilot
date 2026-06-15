---
id: FR-LAYOUT-008
type: FR
area: LAYOUT
provenance: doc
status: unratified
verification: automated
tests: []
why: Global keyboard shortcuts let power users drive the app (create, edit, complete, search) without reaching for the mouse, speeding repetitive task management.
---
The application supports global keyboard shortcuts as specified in REQUIREMENTS.md §4.8: N (new task), E (edit selected task), / (focus search), Esc (close slide-over/modal), ? (show keyboard-shortcuts overlay), and Space (toggle selected task complete); the quick-add bar also supports title+Tab to open the full create form pre-filled.

Status: aspirational — not yet implemented. REQUIREMENTS.md §4.8 specifies the keyboard-shortcut table and the quick-add Tab behaviour, but there is no client-side handler: the project ships no application JavaScript file (only `wwwroot/css/app.css`; no `wwwroot/js/*`), and no `keydown`/`keypress`/keyboard handler exists anywhere outside vendor libraries.
