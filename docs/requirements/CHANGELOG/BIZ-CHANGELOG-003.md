---
id: BIZ-CHANGELOG-003
type: BIZ
area: CHANGELOG
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Services/ChangelogServiceTests.cs"]
why: "Latest version" surfaces (e.g. the sidebar/what's-new hint) must reflect the true newest release, not the lexicographically largest string.
---
The changelog service's "get latest" returns the highest-SemVer version entry (not the lexicographically largest), and returns null when there are no versions.
