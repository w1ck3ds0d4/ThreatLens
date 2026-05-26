# ThreatLens v1 Roadmap

## What v1 is

A log aggregation and correlation engine on .NET Aspire: ingest events from
sources, run regex correlation rules in a background worker, query by
severity / source / time window, and surface results in a Blazor dashboard.
One `dotnet run` boots Postgres + Redis + Ingest API + Query API +
Correlator worker + Dashboard.

## Current state

Ingest, Query, and Correlator services run end-to-end. The Blazor dashboard
is scaffolded with cookie auth and a QueryAPI client; the live event feed UI
is "in progress". Redis is wired but not yet used (pub/sub planned).
Correlator polls Postgres every 5 seconds with 1-second regex timeout guards.
Tests cover `ApiKeyAuth` and `PasswordHasher` only; services and worker lack
integration tests. Bootstrap admin and API keys are seeded but rotation has
no surface. Migrations run on startup (dev-friendly, prod risk).

## v1 acceptance criteria

- [x] Ingest API (`POST /events`, `POST /events/batch`) with API-key auth
- [x] Correlator worker with regex rules + timeout guards + severity elevation
- [x] Query API (events, stats, paginated)
- [x] Blazor dashboard scaffolded with cookie auth
- [x] Aspire stack (Postgres + Redis + pgAdmin + RedisInsight)
- [x] CI gates (restore + build + test)
- [ ] Live event feed UI integrated (SignalR or Redis pub/sub)
- [ ] Rule management API + UI (CRUD, enable/disable, regex test)
- [ ] API key rotation flow (UI or endpoint)
- [ ] User management beyond initial seed (invite + role assignment)
- [ ] Integration tests for Ingest, Query, and Correlator services
- [ ] Production deployment doc (Docker Compose or Helm chart)
- [ ] Migration strategy distinct from startup (Migration job or manual command)
- [ ] Bootstrap secrets handoff via file or setup command (not warning log)
- [ ] Remove `UnitTest1.cs` template file
- [ ] `dotnet format` gate added to CI
- [ ] v1.0.0 tag after manual smoke run

## Milestones to v1

### M1. Live event feed via Redis pub/sub (M)

- [ ] Ingest publishes `Events:New` channel on every accepted event
- [ ] Correlator subscribes for near-real-time matching (alongside 5s polling fallback)
- [ ] Dashboard subscribes via SignalR hub bridging to Redis
- [ ] Dashboard renders new events as they arrive, severity-coloured

**Acceptance:** ingest -> screen latency under 1 second on a dev machine.

### M2. Rule management surface (M)

- [ ] `POST /rules`, `PUT /rules/{id}`, `DELETE /rules/{id}` endpoints, API-key gated
- [ ] Dashboard page listing rules with enable/disable + delete
- [ ] "Test regex" sandbox: paste sample, get match preview + timing
- [ ] Rule changes invalidate Correlator's cache

**Acceptance:** an operator can author, enable, edit, and disable a rule entirely from the UI.

### M3. API key + user lifecycle (M)

- [ ] Generate / list / rotate / revoke API keys
- [ ] Invite user + role assignment (admin / analyst / viewer)
- [ ] Bootstrap secrets exit logs entirely; first run writes a one-shot file

**Acceptance:** all admin tasks possible without `dotnet ef database update` or log scraping.

### M4. Integration tests (M)

- [ ] Testcontainers Postgres + Redis
- [ ] Ingest -> Correlator -> Query happy path
- [ ] Rate-limit / auth-failure paths
- [ ] Correlation timeout enforcement

**Acceptance:** at least 20 integration tests; coverage of the three services' happy + sad paths.

### M5. Production deploy story (M)

- [ ] `docker-compose.prod.yml` covering Ingest / Query / Correlator / Dashboard / Postgres / Redis
- [ ] Separate migration job (manual `dotnet ef database update` or migration container)
- [ ] Production-safe health endpoints (auth-gated, no internal info leak)
- [ ] Documented backup / restore path for Postgres

**Acceptance:** a non-Aspire deployment is reproducible from `docs/deployment.md`.

### M6. Release + tag (S)

- [ ] Smoke test the live feed + rule + user flows
- [ ] CHANGELOG.md
- [ ] `v1.0.0` tag

**Acceptance:** documented smoke checklist passes; tag pushed.

## Beyond v1 (post-1.0 polish)

- Structured payload parsing (currently `RawPayload` is opaque)
- Alerting + webhook sinks (Discord, Slack, PagerDuty)
- SAML / OIDC SSO
- Multi-tenant isolation
- Retention + archival policies

## Out of scope for v1

- Real-time stream processing (5s polling + pub/sub is the v1 architecture)
- Distributed tracing across customer apps (only ThreatLens's own traces are exported)
- ML-based correlation
