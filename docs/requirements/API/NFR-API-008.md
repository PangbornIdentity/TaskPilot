---
id: NFR-API-008
type: NFR
area: API
provenance: doc
status: unratified
verification: automated
why: Deferring rate limiting to iteration 2 is a deliberate scope decision; a documented insertion point keeps it intentional.
tests: []
---
No request rate limiting is enforced in iteration 1; a single documented insertion point exists in `Program.cs` (between `app.UseAuthorization()` and `app.MapControllers()`) where a per-API-key sliding-window limiter is to be added in iteration 2 (per ARCHITECTURE.md §4.8, REQUIREMENTS.md §5.5, CLAUDE.md rule #8).

Status: aspirational — not yet implemented. The insertion point is documented only in ARCHITECTURE.md §4.8; there is no marker comment in `src/Program.cs` itself, and no `UseRateLimiter` call.
