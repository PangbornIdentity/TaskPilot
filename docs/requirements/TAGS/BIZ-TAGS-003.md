---
id: BIZ-TAGS-003
type: BIZ
area: TAGS
provenance: test
status: unratified
why: SUGGESTED: A per-tag task count lets the user gauge how heavily each label is used and identify unused tags worth pruning; excluding deleted tasks keeps the count truthful.
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Services/TagServiceTests.cs", "tests/TaskPilot.Tests.Integration/Tags/TagsApiTests.cs"]
---
Each tag returned in the tag list carries a task count equal to the number of (non-deleted) tasks the tag is assigned to.
