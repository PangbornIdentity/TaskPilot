---
id: FR-TAGS-005
type: FR
area: TAGS
provenance: test
status: unratified
why: A 404 distinguishes a missing resource from a malformed request, giving API clients an accurate, actionable signal instead of a false success.
verification: automated
tests: ["tests/TaskPilot.Tests.Integration/Tags/TagsApiTests.cs"]
---
Updating a tag that does not exist returns not-found (HTTP 404).
