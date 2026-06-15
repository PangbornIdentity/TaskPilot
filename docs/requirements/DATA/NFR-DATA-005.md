---
id: NFR-DATA-005
type: NFR
area: DATA
provenance: code
status: unratified
verification: automated
why: Server-stamped timestamps make CreatedDate/LastModifiedDate authoritative and immune to client clock or tampering.
tests: []
---
On every persist, the DbContext stamps audit timestamps automatically for all `BaseEntity` rows: newly added rows get both `CreatedDate` and `LastModifiedDate` set to the current UTC time, and modified rows get `LastModifiedDate` refreshed — independent of any value supplied by the caller.
