# Edulytics Production Deployment

## Phase 27 release rule

Production Go-Live promotes the exact application artifact accepted by the
protected GitHub pipeline. Build once, identify the exact commit SHA/image, and
deploy that immutable image to production.

Do not treat a successful staging homepage as production acceptance.

## Target initial production topology

The controlled first release uses:

```text
1 paid always-on Render Web Service
  ASP.NET Core MVC/Razor
  SignalR
  Combined runtime role / durable Outbox worker

1 Neon production PostgreSQL branch/database
```

Phase 25 already qualified multi-instance correctness. Initial production may
remain one paid always-on application instance to minimize moving parts. Do not
scale above one web instance unless the accepted shared Data Protection,
SignalR backplane and distributed-limit configuration are enabled as required.

## Database connections

Runtime:

```text
ConnectionStrings__DefaultConnection
```

Use the approved Neon pooled runtime endpoint.

Migration:

```text
ConnectionStrings__MigrationConnection
```

Use a separate Neon direct/non-pooler endpoint.

Never commit either connection string.

## Migration ordering

Production must set:

```text
Edulytics__Deployment__RunStartupMigrations=false
```

Run this as the Render pre-deploy command:

```text
/app/phase27-predeploy.sh
```

Only after the pre-deploy migration succeeds may the new web process become
ready.

The normal production runtime must not apply migrations merely because the
migration connection variable exists.

## Render production service

Production must be a paid always-on service with:

- HTTPS/custom production domain;
- `/health/ready` health check;
- graceful shutdown delay;
- production secrets outside Git;
- exact approved image SHA/digest;
- pre-deploy command `/app/phase27-predeploy.sh`;
- startup migration flag false;
- `ASPNETCORE_ENVIRONMENT=Production`;
- forwarded headers enabled;
- persistent PostgreSQL Data Protection key storage;
- required Data Protection certificate configured;
- pooled runtime DB connection;
- direct migration DB connection;
- SMTP production/test-delivery configuration;
- AllowedHosts overridden to the production host(s).

An image-backed Render service is preferred for explicit promotion of the exact
prebuilt CI image instead of rebuilding an arbitrary branch state during
go-live.

## Health

Verify publicly:

```text
GET /health/live
GET /health/ready
```

Readiness covers the core serving contract, including PostgreSQL and accepted
worker/migration state.

## Rollback

Application rollback must target a previously accepted immutable image.

Do not automatically reverse a production migration. Roll back the application
only when the deployed schema remains backward-compatible with that image.

If data/schema recovery is required, use `docs/BACKUP_RESTORE_RUNBOOK.md`.
