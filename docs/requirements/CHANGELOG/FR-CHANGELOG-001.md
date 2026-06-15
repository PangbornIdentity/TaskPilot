---
id: FR-CHANGELOG-001
type: FR
area: CHANGELOG
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.E2E/Changelog/ChangelogTests.cs", "tests/TaskPilot.Tests.Integration/Changelog/ChangelogPageTests.cs"]
why: Users learn what shipped from /changelog; unrendered template expressions would expose a broken release-notes surface.
---
The application exposes a "/changelog" page that, for an authenticated user, renders the version history including a "What's new" heading and per-version entries showing rendered version numbers (e.g. "v1.2"), never raw/unevaluated template expressions.
