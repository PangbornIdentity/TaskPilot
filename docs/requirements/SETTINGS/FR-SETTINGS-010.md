---
id: FR-SETTINGS-010
type: FR
area: SETTINGS
provenance: doc
status: unratified
verification: automated
tests: []
why: CSV export lets users get their own task data out of the app for backup or analysis, avoiding lock-in.
---
The Settings page provides a Data Export action that exports the user's tasks as a CSV file.

Status: aspirational — not yet implemented. REQUIREMENTS.md §4.6 lists a "Data Export — Export tasks as CSV" item under the Settings page, but no CSV export exists in the code (no export action on the Settings page model `src/Pages/Settings/Index.cshtml.cs`, no markup in `src/Pages/Settings/Index.cshtml`, and no export endpoint/service anywhere under `src/`).
