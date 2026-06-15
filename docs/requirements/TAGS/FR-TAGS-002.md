---
id: FR-TAGS-002
type: FR
area: TAGS
provenance: test
status: unratified
why: SUGGESTED: Listing the user's tags is what populates tag pickers and filters; scoping to the owner keeps one user's labels out of another's view.
verification: automated
tests: ["tests/TaskPilot.Tests.Integration/Tags/TagsApiTests.cs", "tests/TaskPilot.Tests.Unit/Services/TagServiceTests.cs"]
---
A user can list all of their own tags.
