---
id: BIZ-HEALTH-001
type: BIZ
area: HEALTH
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Diagnostics/BuildInfoTests.cs"]
why: Build metadata is the binary's self-report of what version is deployed; graceful "unknown" fallbacks keep diagnostics working on unstamped local builds.
---
BuildInfo reads build metadata from the assembly: Version is a non-empty SemVer-shaped string (Major.Minor.Patch); GitCommit is either a 40-char lowercase hex SHA or "unknown"; GitCommitShort is either 7 chars or "unknown"; BuildTimestampUtc, when stamped, is UTC and within the last 365 days and not in the future; all accessors fall back gracefully to "unknown" without throwing or returning null/empty when metadata is missing.
