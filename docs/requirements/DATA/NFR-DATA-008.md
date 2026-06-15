---
id: NFR-DATA-008
type: NFR
area: DATA
provenance: doc
status: unratified
verification: automated
why: Pinning every project to net10.0 keeps the runtime/toolchain uniform and matches the mandated target framework.
tests: []
---
All project assemblies (the `src` web project and all three test projects) target `net10.0` (per CLAUDE.md rule #9, REQUIREMENTS.md constraint #11). Verified in `src/TaskPilot.csproj` and `tests/*/*.csproj` — every `<TargetFramework>` is `net10.0`.
