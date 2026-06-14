---
id: BIZ-TAGS-001
type: BIZ
area: TAGS
provenance: test
status: unratified
why: Duplicate tag names within one user would make labels ambiguous in pickers and filters; scoping uniqueness per user avoids forcing a global namespace that would collide across unrelated accounts.
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Services/TagServiceTests.cs", "tests/TaskPilot.Tests.Integration/Tags/TagsApiTests.cs"]
---
A tag name must be unique per user: creating or renaming a tag to a name already held by another of the same user's tags is rejected (HTTP 409 / InvalidOperationException). Uniqueness is scoped to the owning user, so two different users may each hold a tag with the same name.
