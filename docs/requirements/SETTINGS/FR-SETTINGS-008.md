---
id: FR-SETTINGS-008
type: FR
area: SETTINGS
provenance: code
status: unratified
verification: automated
tests: []
why: Re-rendering in place with the identity errors lets the user see exactly why a password change failed without losing the rest of the page state.
---
A failed password change on the Settings page re-renders the page in place (no redirect) with the concatenated identity error descriptions shown and the API-keys and tags lists reloaded; a successful change redirects with a success toast.
