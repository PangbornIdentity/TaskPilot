---
id: BIZ-TAGS-002
type: BIZ
area: TAGS
provenance: test
status: unratified
why: Skipping the uniqueness check when the name is unchanged lets a color-only edit succeed, since a tag would otherwise always collide with its own existing name.
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Services/TagServiceTests.cs"]
---
The per-user duplicate-name check is skipped when a tag update does not change the tag's name (e.g. a color-only edit), so re-saving a tag with its existing name does not raise a conflict.
