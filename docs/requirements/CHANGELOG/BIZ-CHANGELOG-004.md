---
id: BIZ-CHANGELOG-004
type: BIZ
area: CHANGELOG
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Services/ChangelogServiceTests.cs"]
why: SUGGESTED: The is-major flag drives the "Major" badge so major releases are visually distinguished from minor/patch ones.
---
A version entry exposes an "is major" flag that is true only when its version type is "major" and false for other version types (e.g. "minor").
