---
id: FR-TAGS-004
type: FR
area: TAGS
provenance: human
status: ratified
why: Users need to retire labels they no longer use; deletion removes the tag from all tasks so stale labels do not clutter pickers and filters.
verification: automated
tests: ["tests/TaskPilot.Tests.Integration/Tags/TagsApiTests.cs", "tests/TaskPilot.Tests.Unit/Services/TagServiceTests.cs", "tests/TaskPilot.Tests.Unit/Services/TagSoftDeleteTests.cs", "tests/TaskPilot.Tests.Integration/Tags/TagSoftDeleteIntegrationTests.cs"]
---
Deleting a tag soft-deletes it (IsDeleted + DeletedAt); it disappears from the user's tag list and from any task's tags while its TaskTag associations are preserved.
