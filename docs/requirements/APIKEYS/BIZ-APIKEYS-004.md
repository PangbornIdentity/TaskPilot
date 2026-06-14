---
id: BIZ-APIKEYS-004
type: BIZ
area: APIKEYS
provenance: code
status: unratified
why: Active-by-default makes a freshly created key immediately usable, while the active/inactive flag gives owners a reversible kill switch enforced at validation.
verification: automated
tests: []
---
A newly created API key is active (IsActive = true) by default; deactivating a key sets IsActive = false and reactivating sets it back to true, and only active keys validate successfully (an explicitly deactivated key is treated as invalid).
