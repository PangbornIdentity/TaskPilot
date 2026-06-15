---
id: FR-SETTINGS-006
type: FR
area: SETTINGS
provenance: code
status: unratified
verification: automated
tests: []
why: Defaulting a blank color to a neutral slate guarantees every tag chip renders a visible color dot rather than appearing broken.
---
When creating or updating a tag with a blank color, the Settings page defaults the color to "#64748B" before validation and persistence.
