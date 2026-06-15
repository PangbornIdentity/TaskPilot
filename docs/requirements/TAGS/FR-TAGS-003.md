---
id: FR-TAGS-003
type: FR
area: TAGS
provenance: test
status: unratified
why: Editing name and color lets the user correct or rebrand a label without recreating it and losing its task associations.
verification: automated
tests: ["tests/TaskPilot.Tests.Integration/Tags/TagsApiTests.cs"]
---
A user can update an existing tag's name and color, and the updated values are returned.
