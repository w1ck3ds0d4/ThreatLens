# How ThreatLens works

ThreatLens is a small log-aggregation and correlation engine. You
send it events from anywhere (a server, an app, a script), it stores
them, runs regex rules over the messages, tags matches, and exposes
the result through a query API and a Blazor dashboard. The whole
stack starts with one `dotnet run` thanks to .NET Aspire.

## What it does

1. Accepts log events over HTTP via the **Ingest API**.
2. Stores them in **Postgres** with indexes on timestamp and severity.
3. A **Correlator** worker scans new events every 5 seconds, applies
   the regex rules you've defined, and elevates severity on matches.
4. Anyone with an API key can read events back via the **Query API**
   (filter by severity, source, id) or hit `/stats` for a 24-hour
   summary.
5. The **Dashboard** (Blazor Server) gives you a logged-in UI on top
   of the Query API.

## Key features

- **One-command local stack.** Aspire spins up Postgres, Redis,
  pgAdmin, RedisInsight, and all four services with traces, metrics,
  and logs visible in the Aspire dashboard.
- **API-key auth on every API endpoint.** Bearer tokens, SHA-256
  hashed at rest, fixed-time compared, with revocation support
  (`src/ThreatLens.Data/ApiKeyAuth.cs`).
- **Regex correlation with timeouts.** Each rule runs with a
  1-second per-match timeout so a malicious or accidentally
  catastrophic pattern can't stall the worker
  (`src/ThreatLens.Correlator.Worker/Worker.cs`).
- **Severity elevation.** A matched rule can promote an event to a
  higher severity (e.g., a "Failed password" log gets bumped from
  Error to Critical).
- **Batch ingestion.** `POST /events/batch` takes an array, useful
  for shippers that buffer and flush.
- **Pagination via stable id cursor.** `GET /events?afterId=...&limit=...`
  with `limit` clamped to 1..500.
- **Cookie-auth dashboard with PBKDF2 passwords.** 200k-iteration
  PBKDF2-SHA256 with random salt
  (`src/ThreatLens.Data/PasswordHasher.cs`).

## Severity levels

| Value | Name |
|---|---|
| 0 | Debug |
| 1 | Info |
| 2 | Warn |
| 3 | Error |
| 4 | Critical |

Defined in `src/ThreatLens.Domain/LogEvent.cs`.

## Getting it running

You need the .NET 10 SDK (`global.json` pins `10.0.100`) and Docker
Desktop. Then:

```bash
dotnet run --project src/ThreatLens.AppHost
```

Aspire prints a localhost URL for its dashboard. Each service is
listed there with its own port, traces, and logs. On first start
the Ingest API logs an "Initial ingest key" once, and the Dashboard
logs a bootstrap admin password (or uses
`THREATLENS_ADMIN_PASSWORD` if you set it before launch). Save both;
they are not retrievable later.

## Sending events

```bash
curl -X POST http://localhost:{ingest-port}/events \
  -H "Authorization: Bearer tl_..." \
  -H "Content-Type: application/json" \
  -d '{
    "source": "sshd",
    "severity": 3,
    "message": "Failed password for root from 10.0.0.7",
    "host": "web-01"
  }'
```

`timestamp` and `rawPayload` are optional. Omitted timestamps default
to `UtcNow`.

## Defining rules

Rules live in the `correlation_rules` table. There is no UI or API
for managing them yet (see `ROADMAP.md`); insert directly:

```sql
insert into correlation_rules (name, pattern, elevate_to, enabled)
values ('failed-ssh', 'Failed password.*from', 4, true);
```

The worker picks up new and changed rules on its next 5-second poll.
Matching is case-insensitive.

## Querying

```bash
# Most recent 50 errors-or-worse
curl -H "Authorization: Bearer tl_..." \
  "http://localhost:{query-port}/events?limit=50&minSeverity=3"

# 24-hour stats
curl -H "Authorization: Bearer tl_..." \
  "http://localhost:{query-port}/stats"
```

`/stats` returns total events, count of uncorrelated events, and a
histogram by severity for the last 24 hours.

## Dashboard

The Blazor Server dashboard is scaffolded under
`src/ThreatLens.Dashboard/Components/`. Cookie auth, login at
`/login`, logout at `/logout`. The live event feed UI is still being
built out; for now the README and code call out that feed integration
is in progress.

## License at a glance

Dual-licensed: AGPLv3 by default (see `LICENSE`), with a commercial
license available for closed-source or hosted use that doesn't want
the AGPL source-disclosure requirement (see `COMMERCIAL.md`).
