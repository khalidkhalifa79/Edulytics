# Edulytics — Phase 13 Closeout

Phase 13 migrates Edulytics from the historical SQL Server runtime/migration path to EF Core + Npgsql + PostgreSQL and validates the target managed PostgreSQL path on a non-production Neon environment.

## Baseline

`f19985f4ee66d11aa014110ae4b8916d64176730`

## Accepted active architecture

- Entity Framework Core
- Npgsql
- PostgreSQL
- Neon target environment
- pooled runtime connection
- direct/non-pooler migration connection

No database credential is stored in Git.

## Acceptance evidence

The Phase 13 full runner proves against real PostgreSQL and Neon:

- EF migration/model consistency;
- real MVC startup and health;
- ASP.NET Identity roles, password reset, lock/unlock and HTTP login;
- SuperAdmin with `SchoolId = null`;
- optimistic concurrency with stale-write rejection;
- Students import;
- Teachers import;
- Classes import;
- Subjects import;
- AssessmentResults import;
- CurriculumMappings import;
- invalid-import rejection;
- duplicate/idempotent upload behavior;
- stale import RowVersion rejection;
- analytics projection refresh and PostgreSQL readback;
- durable Outbox creation and processing;
- realtime notification dispatch path;
- absence of an active SQL Server runtime/provider path;
- dependency vulnerability audit;
- staged secret scan;
- final build and full regression;
- commit/push and clean `HEAD == origin/main`.

## Boundary

Render deployment, production HA/multi-instance behavior, Outbox v2, CI/CD hard gates, production backup/restore evidence and final go-live are later Production Master Plan phases and are not falsely claimed by Phase 13.
