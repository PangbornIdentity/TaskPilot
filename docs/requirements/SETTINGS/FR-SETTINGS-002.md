---
id: FR-SETTINGS-002
type: FR
area: SETTINGS
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.E2E/Settings/SettingsTests.cs"]
why: The raw key is only recoverable at creation since it is stored hashed; showing it once with a copy affordance is the user's sole chance to save it.
---
On the Settings page a user generates an API key by entering a key name and submitting; the newly created key value (e.g. a "tp_"-prefixed token with a copy affordance) is displayed once on creation.
