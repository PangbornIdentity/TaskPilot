---
id: NFR-HEALTH-002
type: NFR
area: HEALTH
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Integration/Smoke/DeploymentSmokeTests.cs"]
why: A parameterizable smoke suite confirms a deploy actually shipped the expected commit and isn't being masked by CDN caching before traffic is trusted.
---
A deployment smoke suite, parameterizable by target base URL (local or Azure) and an optional expected commit, verifies that the deployed instance reports a SemVer-shaped version, matches the expected commit when provided, reports a fully "healthy" status, and serves version responses with no CDN caching (no Age header and a stable commit across cache-busted requests).
