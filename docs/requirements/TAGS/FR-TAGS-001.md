---
id: FR-TAGS-001
type: FR
area: TAGS
provenance: test
status: unratified
verification: automated
why: Tags are the user's own organizing labels; persisting the chosen color is what lets tasks render as recognizable colored pills across the UI.
tests: ["tests/TaskPilot.Tests.Integration/Tags/TagsApiTests.cs", "tests/TaskPilot.Tests.Unit/Services/TagServiceTests.cs"]
---
A user can create a tag with a name and a color, which persists the tag (HTTP 201) and returns it with the supplied color.
