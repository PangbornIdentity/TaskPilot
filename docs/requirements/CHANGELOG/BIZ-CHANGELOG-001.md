---
id: BIZ-CHANGELOG-001
type: BIZ
area: CHANGELOG
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Services/ChangelogServiceTests.cs"]
why: Tolerating empty or malformed release-notes JSON keeps the changelog page from crashing on a hand-edited or partially-deployed file.
---
The changelog service parses the release-notes JSON into version entries (each with version, release date, version type, summary, and a list of changes each having a type and description); valid JSON yields the versions, and empty or malformed JSON yields an empty list without throwing.
