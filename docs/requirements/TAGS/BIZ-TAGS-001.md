---
id: BIZ-TAGS-001
type: BIZ
area: TAGS
provenance: human
status: ratified
why: Duplicate tag names within one user would make labels ambiguous in pickers and filters; scoping uniqueness per user avoids forcing a global namespace that would collide across unrelated accounts. The unfiltered unique DB index enforces this invariant at the storage level — including soft-deleted rows — so recreating a tag by its old name revives the original row (resurrect-on-recreate) rather than inserting a new one that would violate the constraint.
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Services/TagServiceTests.cs", "tests/TaskPilot.Tests.Integration/Tags/TagsApiTests.cs", "tests/TaskPilot.Tests.Integration/Tags/TagResurrectIntegrationTests.cs"]
---
A tag name must be unique per user across both live and soft-deleted rows: the unique index on (UserId, Name) is unfiltered, so a soft-deleted tag still occupies that key. Creating a tag whose name matches a soft-deleted tag for the same user resurrects the original row (IsDeleted → false, DeletedAt → null, Color updated, same Id and preserved TaskTag associations) rather than inserting a new row. Creating a tag whose name matches a live tag for the same user is rejected (HTTP 409 / InvalidOperationException). Uniqueness is scoped to the owning user, so two different users may each hold a tag with the same name.
