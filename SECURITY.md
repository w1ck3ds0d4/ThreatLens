# Security

This document captures ThreatLens's current security posture, known
hardening, threat assumptions, and how to disclose vulnerabilities.
The project is dual-licensed (AGPLv3 / commercial); see `LICENSE`
and `COMMERCIAL.md`.

## Reporting a vulnerability

Email **daniel.svs@outlook.com** with details and, if possible, a
proof of concept. Please do not file public GitHub issues for
security-sensitive reports. A response typically follows within a
few business days.

## Trust boundaries

- **Ingest API → Postgres.** External producers cross this boundary.
  Authenticated via bearer API key
  (`src/ThreatLens.ServiceDefaults/ApiKeyEndpointFilter.cs`).
- **Query API → Postgres.** External consumers cross this boundary.
  Same bearer-key filter on every endpoint.
- **Dashboard browser → Dashboard server.** Authenticated with a
  cookie session (`src/ThreatLens.Dashboard/DashboardAuth.cs`,
  `Cookie.HttpOnly = true`, `SameSite = Strict`).
- **Dashboard server → Query API.** Internal hop. The Dashboard
  attaches its own service-credential bearer key on every request
  via `BearerKeyHandler` in
  `src/ThreatLens.Dashboard/QueryApiCredential.cs`.
- **Correlator Worker → Postgres.** Same-process trust; no external
  callers.

## Authentication and credentials

- **API keys.** 32 random bytes (`RandomNumberGenerator.Fill`) base64-
  url encoded with a `tl_` prefix
  (`src/ThreatLens.Data/ApiKeyAuth.cs`). Only a SHA-256 hash is
  persisted. Lookups compare hashes in fixed time
  (`CryptographicOperations.FixedTimeEquals`). Plaintext keys are
  emitted exactly once at warning-log level. `RevokedAt` short-
  circuits validation. `LastUsedAt` updates are throttled to once
  per 5 minutes to avoid write amplification.
- **Bootstrap ingest key.** Seeded on first Ingest API startup
  (`src/ThreatLens.Ingest.Api/IngestAuth.cs`). Logged once. Cannot
  be retrieved later, only rotated by inserting a new key and
  revoking the old row.
- **Service credentials.** The Dashboard's outbound key is created
  on first run via `EnsureServiceCredentialAsync` and stored in
  `service_credentials.raw_key` in plaintext alongside its hashed
  `api_keys` row, so the Dashboard can read the key back at startup.
  This means anyone with read access to the Postgres `service_credentials`
  table can impersonate the Dashboard against the Query API.
- **Dashboard passwords.** PBKDF2-SHA256, 200,000 iterations, 16-byte
  random salt, 32-byte hash, stored as
  `pbkdf2$iters$saltHex$hashHex` (`PasswordHasher.cs`). The
  iteration count is read out of the stored hash so future bumps
  don't invalidate existing passwords. Verification uses
  `CryptographicOperations.FixedTimeEquals`.
- **Bootstrap admin.** Created on first Dashboard startup if the
  `users` table is empty
  (`DashboardAuth.SeedBootstrapAdminAsync`). Password sourced from
  `THREATLENS_ADMIN_PASSWORD` if present and at least 12 characters,
  otherwise generated (18 random bytes, base64url) and logged once.

## Cookie / session hardening

In `DashboardAuth.AddDashboardAuth`:

- `Cookie.HttpOnly = true`
- `Cookie.SameSite = SameSiteMode.Strict`
- `Cookie.SecurePolicy = SameAsRequest`
- 8-hour expiry with sliding renewal
- Authorization fallback policy requires authentication for all
  endpoints by default

`Program.cs` for the Dashboard also calls `UseHttpsRedirection`,
`UseHsts` (non-development), and `UseAntiforgery`.

## Input handling

- **Regex DoS.** The Correlator runs every rule with a 1-second
  `MatchTimeout`. Patterns that throw `RegexMatchTimeoutException` or
  `ArgumentException` (invalid pattern) are logged and skipped, not
  fatal (`src/ThreatLens.Correlator.Worker/Worker.cs`).
- **SQL injection.** All persistence goes through EF Core; no raw
  SQL is built from user input.
- **Pagination clamps.** The Query API clamps `limit` to 1..500
  (`Math.Clamp` in `src/ThreatLens.Query.Api/Program.cs`), preventing
  unbounded reads.

## Known limitations and threats

- **Plaintext service credential.** As above, the Dashboard's
  service key is stored as plaintext. Acceptable for single-host
  dev, not appropriate for shared production databases.
- **No rate limiting.** Neither API uses ASP.NET rate limiting
  middleware, so a stolen key has no throttling between it and
  Postgres.
- **Migrations on startup.** Every service runs
  `db.Database.MigrateAsync()` on boot. A compromised service
  account therefore has DDL rights on the database.
- **Health endpoints.** `/health` and `/alive` are mapped only in
  development (`ServiceDefaults/Extensions.cs`); production exposure
  needs a deliberate decision.
- **No audit log.** API key use, login attempts, and rule changes
  are not separately audited beyond standard request logs.
- **Bootstrap secrets in logs.** Initial ingest key and bootstrap
  admin password are written to the application log. Anyone with
  access to that log stream during first start can read them.
