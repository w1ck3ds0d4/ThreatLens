# Roadmap

**Status:** maintain. **Last reviewed:** 2026-09-24.

ThreatLens is a portfolio/technical-demo repo (a .NET Aspire log aggregation
and correlation engine). It is not the paid product, so it stays frozen:
"done" for now means CI and Dependabot both stay green with no scheduled
feature work, not shipping the two remaining v1 items.

> How this file is used: Claude Project threads build the first unticked item under **Now**, one item per branch and pull request, and tick it in that same PR as `- [x] ... (#PR)`. Daniel owns the order and the lists; threads never add to Now, Next or Later themselves, they propose under **Ideas**.

## Now

No new features without Daniel's go.

- [ ] **Keep CI green**: watch `ci.yml` (restore/build/test) and `security.yml` on every PR; fix a break only if it is caused by a change in this repo. Done when: the last 5 runs on `main` are green.
- [ ] **Keep Dependabot patched**: merge routine dependency-bump PRs once their own CI passes. Done when: 0 open Dependabot alerts.

## Next

- [ ] **Live event feed (parked)**: wire Redis pub/sub so the Ingest API publishes an `Events:New` channel and the Correlator/Dashboard subscribe for near-real-time updates, replacing the current 5-second poll. Done when: ingest-to-screen latency is under 1 second on a dev machine.
- [ ] **Rule management API + UI (parked)**: `POST/PUT/DELETE /rules` endpoints plus a Dashboard page to list, enable/disable and regex-test correlation rules. Done when: an operator can author, enable, edit and disable a rule entirely from the UI.

## Later

- API key rotation flow (UI or endpoint), beyond the current seed-only bootstrap.
- User management beyond the initial seed (invite + role assignment).
- Integration tests for Ingest, Query and Correlator (Testcontainers Postgres + Redis).
- Production deployment doc distinct from the Aspire dev stack (Docker Compose or Helm, separate migration job).
- Structured payload parsing (currently `RawPayload` is opaque).
- Alerting/webhook sinks, SAML/OIDC SSO, multi-tenant isolation, retention policy.

## Ideas

(empty; threads add proposals here)

## Done

- [x] Aspire stack: one `dotnet run` boots Postgres, Redis, Ingest API, Query API, Correlator worker, and the Blazor dashboard
- [x] Ingest API (`POST /events`, `POST /events/batch`) with API-key auth
- [x] Correlator worker with regex rules, timeout guards, and severity elevation
- [x] Query API (events, stats, paginated)
- [x] Blazor dashboard scaffolded with cookie auth and a QueryAPI client
- [x] CI gates (restore + build + test) and CodeQL scanning, currently green with 0 open Dependabot alerts
- [x] Repo-wide em dash to hyphen cleanup (#86, #87)
