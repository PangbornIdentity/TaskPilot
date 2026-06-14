---
id: FR-DASHBOARD-006
type: FR
area: DASHBOARD
provenance: code
status: unratified
verification: automated
tests: []
why: Sensible fixed defaults let one-field quick-add produce a valid task instantly, while ignoring blank titles prevents accidental empty tasks.
---
Dashboard quick-add ignores a null or whitespace-only title (redirecting without creating a task); a valid title is trimmed and creates a task with fixed defaults — task type 1 (Task), area Personal, priority Medium, status NotStarted, target-date-type ThisWeek, not recurring — and surfaces a "created" toast.
