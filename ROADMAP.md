# Roadmap

ThreatLens is an early-stage project. The pipeline (ingest, correlate,
query) works end-to-end, but several pieces are scaffolded or
deliberately minimal. This document tracks known gaps and direction.

## Current status

- Ingest API: working, single + batch endpoints, API-key gated.
- Correlator: working, 5-second polling loop with regex timeout
  guards (`src/ThreatLens.Correlator.Worker/Worker.cs`).
- Query API: working, three endpoints, API-key gated.
- Dashboard: scaffolded Blazor Server app with cookie auth and an
  authenticated HTTP client to the Query API. Pages and the live
  event feed are still being built out
  (`src/ThreatLens.Dashboard/Components/`); README notes "feed
  integration in progress".

## Near-term gaps

1. **Live event feed UI.** Razor components under
   `src/ThreatLens.Dashboard/Components/Pages/` need to consume the
   Query API and render a streaming feed. A tail or SignalR-style
   push is preferable over polling.
2. **Redis pub/sub.** Redis is wired into the AppHost
   (`src/ThreatLens.AppHost/AppHost.cs`) and clients are registered
   in Ingest, Correlator, and Dashboard, but no producer or
   subscriber code exists yet. Intended use: Ingest publishes new
   event ids so the Correlator can react in near-real-time instead of
   polling every 5 seconds.
3. **Rule management surface.** Correlation rules currently have to
   be inserted directly into the `correlation_rules` table (see
   README example). There is no API or UI for listing, editing,
   testing, enabling, or disabling rules.
4. **API key management.** `ApiKey` rows can only be created
   programmatically (initial bootstrap key in
   `src/ThreatLens.Ingest.Api/IngestAuth.cs`, service credentials in
   `src/ThreatLens.Data/ApiKeyAuth.cs`). There is no UI or admin
   endpoint to mint, list, rotate, or revoke external keys.
5. **User management.** `DashboardAuth.SeedBootstrapAdminAsync` seeds
   one admin. There are no flows for creating additional users,
   resetting passwords, or deactivating accounts.

## Mid-term ideas

- **Structured payload parsing.** Today `RawPayload` is just an
  opaque string. A pluggable parser (syslog, Windows Event Log JSON,
  CEF) would let rules match on structured fields, not just the
  message blob.
- **Richer correlations.** The current rule model is one regex per
  rule, single match wins (the inner `foreach` breaks on first hit
  in `Worker.cs`). Multi-event correlations (windowed aggregates,
  sequences, thresholds) would unlock more useful detections.
- **Alerting / sinks.** No outbound notifications today. A simple
  webhook or email sink for elevated events is a natural next step.
- **Retention.** No event TTL or archival. Postgres will grow
  unbounded.
- **Authn for service-to-service.** The Dashboard reads its bearer
  key from `service_credentials.raw_key` in plaintext (see
  `ApiKeyAuth.EnsureServiceCredentialAsync`). For multi-tenant or
  hardened deployments this should move to a secrets store or rely
  on Aspire-managed secrets.

## Things explicitly not built

- No Kubernetes / Helm / containerised production deployment is in
  the repo. The AppHost is for local dev.
- No CI workflow files were found in the repo root.
- No load-testing, benchmark, or fuzzing harnesses.
- Tests cover `ApiKeyAuth` and `PasswordHasher`
  (`tests/ThreatLens.Tests/`); the services and worker do not yet
  have integration tests.
- `UnitTest1.cs` (the xUnit template file) is still present in the
  test project and should be removed.

## Operational gaps

- Migrations run on startup (each service that touches
  `ThreatLensDbContext` calls `db.Database.MigrateAsync()`). For
  production this should move to a single, opt-in migration step.
- Health endpoints `/health` and `/alive` are mapped only in
  development (`src/ThreatLens.ServiceDefaults/Extensions.cs`); a
  production strategy is not yet defined.
- Bootstrap secrets (initial ingest key, generated admin password)
  are emitted via warning-level logs once. A nicer onboarding flow
  would print them to a one-shot file or require an explicit setup
  command.
