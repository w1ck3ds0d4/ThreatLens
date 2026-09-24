# Changelog

All notable changes to this project are documented here. Format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/). No version has been
tagged yet.

## Unreleased

### Changed

- Repo-wide em dash to hyphen cleanup, including a follow-up fix that restored a vendored Bootstrap
  file the cleanup had touched by mistake (#86, #87)
- ROADMAP.md refreshed with v1 acceptance criteria and milestones
- License section normalized to plain-text dual-license form (#74, #76)
- Dependabot schedule set to monthly; routine dependency bumps across Aspire, EF Core, Npgsql, and
  the test stack

### Added

- Ingest API, Query API, Correlator worker, and Blazor dashboard scaffolding on .NET Aspire
- CI (`ci.yml`) running restore, build, and tests; CodeQL scanning (`security.yml`)
