---
id: NFR-TASKS-002
type: NFR
area: TASKS
provenance: test
status: unratified
verification: automated
why: Preventing horizontal overflow across supported widths keeps the list usable on small screens and meets WCAG 1.4.10 Reflow.
tests: ["tests/TaskPilot.Tests.E2E/Tasks/TaskLifecycleTests.cs"]
---
The Tasks list page does not overflow horizontally at any supported viewport width from 320px through 1440px (WCAG 1.4.10 Reflow).
