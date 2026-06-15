---
id: FR-SETTINGS-001
type: FR
area: SETTINGS
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.E2E/Settings/SettingsTests.cs"]
why: Settings is the single place a user manages account access — API keys, appearance, and password — so all three sections must be present and functional.
---
The "/settings" page presents an API Keys section, an Appearance section, and a Change Password section (with a current-password input), all rendering without an unhandled error.
