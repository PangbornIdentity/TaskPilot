---
id: FR-SETTINGS-004
type: FR
area: SETTINGS
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.E2E/Settings/SettingsTests.cs"]
why: Blocking duplicate tag names inline (keeping the row open) prevents ambiguous tags while letting the user fix the name without losing context.
---
Renaming a tag to a name that already exists is rejected with an inline "already exists" error, and the edit row remains open with the failing input still visible (no redirect).
