---
id: FR-SETTINGS-007
type: FR
area: SETTINGS
provenance: code
status: unratified
verification: automated
tests: []
why: Scoping the edit row to the user's own tags stops a guessed or tampered editTagId from opening or exposing another user's tag.
---
The Settings page only treats the "editTagId" query parameter (or the tag being edited after a validation/conflict error) as the open edit row when that tag actually belongs to the current user's tag set; otherwise no edit row is opened.
