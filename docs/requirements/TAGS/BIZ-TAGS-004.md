---
id: BIZ-TAGS-004
type: BIZ
area: TAGS
provenance: test
status: unratified
why: Rejecting empty names and non-hex colors at the boundary keeps unrenderable or unlabeled tags out of the store, so the UI can always display a valid pill.
verification: automated
tests: ["tests/TaskPilot.Tests.Integration/Tags/TagsApiTests.cs"]
---
A tag create/update request with an invalid payload — an empty name or a color that is not a valid hex color — is rejected as a bad request (HTTP 400).
