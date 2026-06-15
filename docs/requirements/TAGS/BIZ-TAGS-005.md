---
id: BIZ-TAGS-005
type: BIZ
area: TAGS
provenance: code
status: unratified
why: SUGGESTED: A 50-character cap keeps tag pills compact in the UI and bounds the stored column; enforcing it in both validation and the schema prevents over-long names from slipping in through either path.
verification: automated
tests: []
---
A tag name must not exceed 50 characters; a create or update request whose name is longer is rejected as a bad request (HTTP 400). The 50-character limit is enforced both by request validation and by the persisted column definition.
