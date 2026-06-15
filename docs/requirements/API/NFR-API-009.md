---
id: NFR-API-009
type: NFR
area: API
provenance: doc
status: unratified
verification: automated
why: Restricting Swagger to Development avoids exposing the full API surface map in production.
tests: []
---
The Swagger / OpenAPI UI is served at `/swagger` only when the host environment is Development; it is not mapped in any other environment (per REQUIREMENTS.md §5.5). Implemented in `src/Program.cs` via an `app.Environment.IsDevelopment()` guard around `UseSwagger()` / `UseSwaggerUI()`.
