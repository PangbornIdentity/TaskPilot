---
id: FR-INTEGRATIONS-001
type: FR
area: INTEGRATIONS
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.E2E/Integrations/IntegrationsPageTests.cs"]
why: The Integrations page is how users learn to connect LLMs/automation to the API, so it must be reachable from the sidebar and load cleanly.
---
The application exposes an "/integrations" page that loads for an authenticated user without an unhandled error or 404, and the sidebar contains a navigation link to it.
