# ThreatLens

Log aggregation and correlation engine built on .NET Aspire. Ingest events from any source, run rule-based correlation in the background, query through an API, and watch it all in a Blazor dashboard. Postgres for storage, Redis for signaling, OpenTelemetry everywhere.

## Architecture

```
  clients
    |
    v
  Ingest.Api  ---->  Postgres (threatlens)
                         ^
                         |
  Correlator.Worker  ----+----  rule-based matching, severity elevation
                         |
  Query.Api  <-----------+
    ^
    |
  Dashboard (Blazor Server)
```

Everything orchestrated by `ThreatLens.AppHost`, which spins up Postgres, Redis, pgAdmin, RedisInsight, and all four services. Start the AppHost and the Aspire dashboard at localhost gives you logs, traces, and metrics for the whole stack.

## Projects

| Project | Purpose |
|---|---|
| `ThreatLens.AppHost` | Aspire orchestrator - brings up Postgres, Redis, and the 4 services |
| `ThreatLens.ServiceDefaults` | Shared OTel, health checks, resilient HTTP, service discovery |
| `ThreatLens.Domain` | POCO entities (`LogEvent`, `CorrelationRule`, `Severity`) |
| `ThreatLens.Data` | EF Core `DbContext`, migrations, Postgres provider |
| `ThreatLens.Ingest.Api` | POST `/events` and `/events/batch` - accepts logs |
| `ThreatLens.Correlator.Worker` | Pulls uncorrelated events, runs regex rules, elevates severity |
| `ThreatLens.Query.Api` | GET `/events`, `/events/{id}`, `/stats` - read-side |
| `ThreatLens.Dashboard` | Blazor Server UI - live event feed + stats |

## Running

Prerequisites: .NET 9 SDK, Docker Desktop (for Postgres/Redis containers Aspire spins up).

```bash
dotnet run --project src/ThreatLens.AppHost
```

Aspire dashboard opens on https://localhost:17099 (or whatever port it assigns). From there you can reach each service, check traces, and inspect logs.

## Sending a test event

```bash
curl -X POST http://localhost:{ingest-port}/events \
  -H "Content-Type: application/json" \
  -d '{"source":"sshd","severity":3,"message":"Failed password for root from 10.0.0.7","host":"web-01"}'
```

## Adding correlation rules

Insert rows into the `correlation_rules` table (or seed via EF). The worker picks them up on its next poll (5s interval).

```sql
insert into correlation_rules (name, pattern, elevate_to, enabled)
values ('failed-ssh', 'Failed password.*from', 4, true);
```

The worker will mark matching events, elevate them to the rule's severity, and stamp `matched_rule`.

## Status

Initial scaffold. Not production-ready. Migrations, auth, rule CRUD UI, and the Blazor event feed are still TODO.

## License

MIT
