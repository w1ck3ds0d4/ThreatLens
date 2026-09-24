# ThreatLens

A .NET Aspire log aggregation and correlation engine: Ingest API, Query API, a
Correlator worker running regex rules, and a Blazor dashboard, backed by
Postgres and Redis.

## Commands

```bash
dotnet restore
dotnet build
dotnet test tests/ThreatLens.Tests/ThreatLens.Tests.csproj

dotnet run --project src/ThreatLens.AppHost        # full Aspire stack
dotnet run --project src/ThreatLens.Ingest.Api      # a single service, without Aspire
```

CI (`ci.yml`) runs restore, `dotnet build --configuration Release`, then the test project on
every push and PR to `main`. There is no separate lint step.

## Layout

| Path | What it is |
| --- | --- |
| `src/ThreatLens.AppHost` | Aspire orchestrator; boots Postgres, Redis, and all services |
| `src/ThreatLens.Ingest.Api` | Accepts events (`POST /events`, `/events/batch`), API-key auth |
| `src/ThreatLens.Query.Api` | Read side: events, stats, pagination |
| `src/ThreatLens.Correlator.Worker` | Background worker running regex correlation rules |
| `src/ThreatLens.Dashboard` | Blazor UI, cookie auth, Query API client |
| `src/ThreatLens.Data` | EF Core DbContext and migrations |
| `src/ThreatLens.Domain` | Shared domain models |
| `src/ThreatLens.ServiceDefaults` | Aspire service defaults (OpenTelemetry, health checks) |
| `tests/ThreatLens.Tests` | Unit tests (currently `ApiKeyAuth` and `PasswordHasher` only) |

## Conventions

- Commits: `(type) lowercase summary`, no trailing period, no body. Types: `feat`, `fix`, `chore`,
  `docs`, `refactor`, `test`.
- ASCII hyphens only, no em dashes or en dashes anywhere.
- Feature branch per change, PR per branch; do not push to `main` directly.
- Status is **maintain** (see `ROADMAP.md`): no new feature work without Daniel's go, keep CI and
  Dependabot green.

## Do not read

`bin/`, `obj/`, `**/bin/`, `**/obj/` (build output), any `*.db`/`*.sqlite` file under `data/` if
present, and `.vs/`.
