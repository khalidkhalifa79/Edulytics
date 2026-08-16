# Edulytics Production Deployment

## Database architecture

Edulytics uses .NET 10, ASP.NET Core, Entity Framework Core, Npgsql and PostgreSQL.
Neon is the target managed PostgreSQL platform. SQL Server is not an active runtime provider.

## Connection separation

Runtime configuration:

```text
ConnectionStrings__DefaultConnection
```

For Neon use the pooled runtime endpoint.

Migration configuration:

```text
ConnectionStrings__MigrationConnection
```

For Neon use the direct/non-pooler endpoint. Do not run schema migrations through the pooled endpoint.
Never commit either database connection string.

## Migration

Linux/macOS/Codespaces:

```text
EDULYTICS_MIGRATION_CONNECTION="<secure direct PostgreSQL connection>" ./scripts/update-database.sh
```

PowerShell:

```text
$env:EDULYTICS_MIGRATION_CONNECTION = "<secure direct PostgreSQL connection>"
.\scripts\update-database.ps1
```

## Build

```text
dotnet restore Edulytics.sln
dotnet build Edulytics.sln -c Release --no-restore
dotnet test Edulytics.sln -c Release --no-build --no-restore
dotnet publish src/Edulytics.Web/Edulytics.Web.csproj -c Release --no-restore
```

## Health

Verify:

```text
GET /health/live
GET /health/ready
```

Readiness covers PostgreSQL connectivity, EF migration state and Outbox worker heartbeat.

## Identity and tenancy

ASP.NET Core Identity is persisted in PostgreSQL through EF Core/Npgsql.
Public registration remains disabled. SuperAdmin is platform-scoped with `SchoolId = null`.
School users remain scoped to one school.

## PostgreSQL concurrency

Phase 13 keeps the existing `byte[] RowVersion` contracts but uses the accepted PostgreSQL-compatible application-managed optimistic-concurrency strategy. Stale writes must fail rather than silently overwrite newer state.

## Neon Phase 13 acceptance

Phase 13 validates a non-production Neon environment with:

- direct migration connection;
- pooled runtime connection;
- Identity persistence and login;
- real PostgreSQL concurrency;
- all six import types;
- analytics refresh/readback;
- Outbox processing;
- realtime notification dispatch path;
- MVC runtime and health endpoints.

Production HA, multi-instance behavior, CI/CD hard gates, backup/restore evidence and final go-live are handled by later Production Master Plan phases.

## Data Protection

Persistent shared ASP.NET Core Data Protection key storage is required before restart-sensitive or multi-instance production acceptance.

## Rollback

Do not automatically reverse production database migrations. Prefer backward-compatible application/schema releases and the later backup/restore runbook.
