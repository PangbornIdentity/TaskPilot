---
id: FR-TAGS-004
type: FR
area: TAGS
provenance: test
status: unratified
why: Users need to retire labels they no longer use; deletion removes the tag from all tasks so stale labels do not clutter pickers and filters.
verification: automated
tests: ["tests/TaskPilot.Tests.Integration/Tags/TagsApiTests.cs", "tests/TaskPilot.Tests.Unit/Services/TagServiceTests.cs"]
---
A user can delete one of their own tags (HTTP 204).
