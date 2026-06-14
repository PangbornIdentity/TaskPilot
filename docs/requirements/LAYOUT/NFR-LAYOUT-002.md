---
id: NFR-LAYOUT-002
type: NFR
area: LAYOUT
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.E2E/Mobile/MobileLayoutTests.cs"]
why: The hamburger/backdrop open-close cycle is the only way mobile users reach navigation, so it must open, dismiss, and route reliably.
---
At mobile viewport, tapping the hamburger opens the sidebar (and shows an active backdrop); tapping the backdrop closes the sidebar; and the user can navigate via a sidebar link once it is open.
