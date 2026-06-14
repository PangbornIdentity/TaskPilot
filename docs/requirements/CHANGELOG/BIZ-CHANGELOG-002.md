---
id: BIZ-CHANGELOG-002
type: BIZ
area: CHANGELOG
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Services/ChangelogServiceTests.cs"]
why: Ordinal-string sorting would place v1.9 above v1.10; numeric SemVer ordering ensures the newest release shows first.
---
The changelog service orders versions in descending SemVer order by parsing each version into numeric (major, minor, patch) components rather than ordinal-string comparison, so "1.10" sorts above "1.9" above "1.8" and "1.10.10" above "1.10.1" above "1.10.0"; a version that cannot be parsed falls back to zero components and sorts after all well-formed entries.
