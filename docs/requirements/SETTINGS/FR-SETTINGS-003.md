---
id: FR-SETTINGS-003
type: FR
area: SETTINGS
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.E2E/Settings/SettingsTests.cs"]
why: Inline rename/recolor lets users curate tags in place, and keyboard-only completion keeps the flow accessible without a mouse.
---
On the Settings page a user can create a tag (name) and then edit it inline to rename and recolor it; the changes persist and are reflected across the page. The inline edit can be completed keyboard-only (focus the edit affordance, press Enter to open, type, press Enter to save).
