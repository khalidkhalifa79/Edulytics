# Edulytics Production Deployment

## Deployment model

Deploy an immutable tested Release build.

Do not modify source code directly on the production host.

## Runtime requirements

- .NET 10 ASP.NET Core runtime unless using self-contained publishing.
- SQL Server compatible with the configured EF Core SQL Server provider.
- HTTPS termination on the application host or trusted reverse proxy.
- Persistent ASP.NET Core Data Protection key storage.
- Central collection of structured JSON application logs.
- Scheduled SQL Server backups.

## Required production secret

The database connection string must be supplied outside Git:

ConnectionStrings__DefaultConnection

If SMTP invitation delivery is enabled, SMTP credentials must also come
from the deployment secret mechanism.

Never place production passwords, tokens, connection strings, or SMTP
credentials in appsettings.Production.json.

## Build

Run:

dotnet restore Edulytics.sln

dotnet build Edulytics.sln -c Release --no-restore

dotnet test Edulytics.sln -c Release --no-build --no-restore

dotnet publish src/Edulytics.Web/Edulytics.Web.csproj -c Release --no-restore

## Database migration

Production database migrations are an explicit deployment step.

Apply reviewed migrations before routing traffic to the new version.

The application readiness endpoint reports Unhealthy while EF Core
detects pending migrations.

## Health verification

After application startup verify:

GET /health/live

GET /health/ready

Both must return HTTP 200 and Healthy.

Readiness checks:

- SQL connectivity;
- EF migration state;
- Outbox worker heartbeat.

## HTTPS and reverse proxy

Forward:

- X-Forwarded-For
- X-Forwarded-Proto
- X-Forwarded-Host

Only trusted proxies should be accepted in normal production hosting.

The Codespaces proxy relaxation is for Codespaces only.

Production external traffic must use HTTPS.

HSTS is enabled outside Development.

## Deployment sequence

1. Verify a recent usable backup.
2. Preserve the currently deployed release reference.
3. Apply reviewed database migrations.
4. Start the new release.
5. Verify liveness.
6. Verify readiness.
7. Route traffic.
8. Monitor HTTP failures, readiness, SQL connectivity and Outbox retries.

## Rollback

Do not automatically reverse production database migrations.

If the previous application release is schema-compatible, the
application may be rolled back independently.

If database recovery is required, use the tested backup/restore runbook.
