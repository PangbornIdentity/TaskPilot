---
id: NFR-DATA-002
type: NFR
area: DATA
provenance: test
status: unratified
verification: automated
why: Stamping LastModifiedBy on every mutation gives every change a traceable actor for accountability.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs"]
---
Every mutation stamps `LastModifiedBy` with the actor provenance string supplied by the caller (`"user:{username}"` for web actions, `"api:{apiKeyName}"` for API actions); the persisted value matches the provenance passed to the service.
