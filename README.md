# ThreatLens

Log aggregation and correlation engine built on .NET Aspire - Postgres, Redis, EF Core, Blazor. Distributed observability included.

---

## Features

- **Ingest API** - POST `/events` and `/events/batch` to accept log events from any source with severity, host, message, and raw payload
- **Background correlator** - polls uncorrelated events, runs regex rules against messages, tags matches and elevates severity
- **Query API** - filter events by severity, source, and ID with pagination; 24h stats endpoint
- **Blazor dashboard** - live event feed UI (scaffolded, feed integration in progress)
- **.NET Aspire orchestration** - one `dotnet run` starts Postgres, Redis, pgAdmin, RedisInsight, and all four services
- **OpenTelemetry everywhere** - unified traces, metrics, logs across services via Aspire dashboard
- **EF Core + PostgreSQL** - code-first schema with indexes on timestamp and severity for fast queries
- **Redis** - wired for future pub/sub between ingest and correlator

---

## Setup

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) - Aspire spins up Postgres and Redis as containers
- (optional) [VS Code](https://code.visualstudio.com/) with the C# Dev Kit extension

### Clone and build

```bash
git clone https://github.com/w1ck3ds0d4/ThreatLens.git
cd ThreatLens
dotnet restore
dotnet build
```

### Run the full stack

```bash
dotnet run --project src/ThreatLens.AppHost
```

Aspire dashboard opens on a localhost port (printed in the console). From there you can reach each service, inspect traces, and watch logs.

### Run individual services (without Aspire)

Useful when iterating on a single service. Configure your own Postgres/Redis connection strings in the service's `appsettings.Development.json`, then:

```bash
dotnet run --project src/ThreatLens.Ingest.Api
```

---

## Usage

### Send a single event

```bash
curl -X POST http://localhost:{ingest-port}/events \
  -H "Content-Type: application/json" \
  -d '{
    "source": "sshd",
    "severity": 3,
    "message": "Failed password for root from 10.0.0.7",
    "host": "web-01"
  }'
```

### Severity levels

| Value | Name |
|---|---|
| 0 | Debug |
| 1 | Info |
| 2 | Warn |
| 3 | Error |
| 4 | Critical |

### Add a correlation rule

Rules are rows in the `correlation_rules` table. The worker polls them every 5 seconds.

```sql
insert into correlation_rules (name, pattern, elevate_to, enabled)
values ('failed-ssh', 'Failed password.*from', 4, true);
```

Events matching the regex will be tagged with `matched_rule = 'failed-ssh'` and bumped to Critical severity.

### Query recent events

```bash
curl "http://localhost:{query-port}/events?limit=50&minSeverity=3"
```

### 24h stats

```bash
curl "http://localhost:{query-port}/stats"
```

---

## Project Structure

```
ThreatLens/
  ThreatLens.sln
  global.json                                  Pins .NET 9 SDK
  src/
    ThreatLens.AppHost/                        Aspire orchestrator
      AppHost.cs                               Wires Postgres, Redis, services
    ThreatLens.ServiceDefaults/                Shared OTel, health, resilience
      Extensions.cs                            AddServiceDefaults, MapDefaultEndpoints
    ThreatLens.Domain/                         POCO entities
      LogEvent.cs                              LogEvent, CorrelationRule, Severity
    ThreatLens.Data/                           EF Core persistence
      ThreatLensDbContext.cs                   DbContext with indexes
    ThreatLens.Ingest.Api/                     POST /events, /events/batch
      Program.cs                               Minimal API endpoints
    ThreatLens.Correlator.Worker/              Background rule matching
      Program.cs                               Hosted service registration
      Worker.cs                                Polling + regex correlation
    ThreatLens.Query.Api/                      Read-side API
      Program.cs                               GET /events, /events/{id}, /stats
    ThreatLens.Dashboard/                      Blazor Server UI
      Program.cs                               Razor component host
      Components/                              Pages and layout
  tests/
    ThreatLens.Tests/                          xUnit test project
```

---

## License

This project is licensed under the [MIT License](LICENSE).
