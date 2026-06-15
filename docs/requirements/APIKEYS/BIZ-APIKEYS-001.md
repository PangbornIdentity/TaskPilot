---
id: BIZ-APIKEYS-001
type: BIZ
area: APIKEYS
provenance: test
status: unratified
verification: automated
why: Storing only a keyed hash means a database leak cannot expose usable credentials, per the never-store-plaintext rule.
tests: ["tests/TaskPilot.Tests.Unit/Services/ApiKeyServiceTests.cs"]
---
On key generation the service persists only an HMAC-SHA256 hash of the plain-text key (a 64-character lowercase hex string) — never the plain-text key — and the stored hash equals HMAC-SHA256 of the plain-text key under the configured secret.
