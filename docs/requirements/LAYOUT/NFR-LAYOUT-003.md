---
id: NFR-LAYOUT-003
type: NFR
area: LAYOUT
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.E2E/Mobile/MobileLayoutTests.cs"]
why: On tablets a persistent icon rail keeps navigation always visible without consuming the width a full sidebar would, matching the medium-screen breakpoint.
---
At tablet viewport width (e.g. 768x1024) the sidebar is displayed as a persistent icon-only rail (sidebar visible, brand text hidden) and no mobile header / hamburger is shown.
