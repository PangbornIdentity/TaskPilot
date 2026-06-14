---
id: NFR-LAYOUT-001
type: NFR
area: LAYOUT
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.E2E/Mobile/MobileLayoutTests.cs"]
why: On a narrow phone screen, hiding the sidebar behind a hamburger reclaims space for content while keeping navigation one tap away.
---
At mobile viewport width (e.g. 390x844) the app shows a mobile header with a hamburger button and the sidebar is hidden off-screen by default.
