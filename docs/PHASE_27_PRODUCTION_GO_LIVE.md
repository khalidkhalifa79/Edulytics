# Edulytics — Phase 27 Production Go-Live

## Authoritative baseline

Phase 27 starts from accepted Phase 26 main:

`4ca72a359f83ab007732d080dcf89bf09670d282`

Repository:

`khalidkhalifa79/Edulytics`

## Hard preconditions

Production cannot be accepted until all are evidenced:

- production-readiness score >= 85/100;
- zero open P0 blockers;
- accepted/documented P1 list;
- production migration rehearsed;
- rollback path documented;
- backup/recovery capability configured;
- monitoring alerts live and tested;
- on-call/incident owner defined;
- production secrets loaded outside Git;
- production DNS/TLS ready.

## Initial release topology

The initial controlled production release is deliberately simple:

- one paid always-on Render web service;
- runtime role `Combined`;
- ASP.NET Core MVC/Razor + SignalR;
- durable Outbox worker in the same process;
- one isolated Neon production database/branch;
- pooled Neon runtime connection;
- separate direct/non-pooler migration connection.

Phase 25 already proves scale-out correctness. Production does not need to start
with multiple web instances. If production later scales above one web instance,
the accepted Redis/backplane, shared Data Protection and distributed-sensitive
rate-limit requirements apply.

## Immutable release

The protected GitHub CI builds and pushes the exact main SHA image.

Production must deploy the exact accepted SHA/digest. Do not rebuild a mutable
branch and call it the promoted artifact.

## Migration order

1. Render obtains the candidate immutable image.
2. `/app/phase27-predeploy.sh` runs on a pre-deploy instance.
3. The script uses only `ConnectionStrings__MigrationConnection`.
4. The migration connection must be direct/non-pooler.
5. A migration failure aborts the deploy.
6. Production startup migrations remain disabled.
7. The new web process starts only after migration succeeds.
8. `/health/ready` must become HTTP 200 before traffic acceptance.

## Production environment contract

Required categories include:

- `ASPNETCORE_ENVIRONMENT=Production`;
- `AllowedHosts` set to real production host(s);
- forwarded headers enabled;
- persistent Data Protection application name/certificate configuration;
- `ConnectionStrings__DefaultConnection` = pooled Neon runtime;
- `ConnectionStrings__MigrationConnection` = direct Neon migration;
- `Edulytics__Deployment__RunStartupMigrations=false`;
- production SMTP connector configuration;
- release SHA/version metadata;
- runtime role configuration appropriate to the chosen topology.

Secrets are never stored in Git.

## Go-live verification

After deployment, verify:

- `/health/live`;
- `/health/ready`;
- public security headers;
- English UI;
- Polish UI;
- SuperAdmin login/operations;
- SchoolAdmin login and tenant-scoped dashboard;
- SignalR connection and reconciliation path;
- durable Outbox processing;
- audit persistence/readback;
- report/export;
- designated external email delivery;
- worker state/heartbeat;
- dead-letter operational access;
- release SHA;
- migration version;
- logs/correlation search;
- alert delivery.

## Hard rejection rules

Do not close Phase 27 if any of these is true:

- homepage works but worker is dead;
- a migration is pending or failed;
- SignalR is broken;
- backup/recovery capability is absent;
- alerting is unavailable;
- authentication cookie fails after restart;
- cross-tenant isolation fails;
- concurrency behavior regresses;
- dead-letter handling is unavailable;
- production release cannot be tied to an exact immutable image.

## Rollback

Application rollback uses a previously accepted immutable image only.

Do not automatically reverse the production database migration. If the current
schema is not backward-compatible with the rollback image, stop and use the
documented recovery path instead of forcing the rollback.

## Current Phase 27 state

This document and the local Phase 27 code establish the production deployment
contract and eliminate known pre-go-live gaps.

Phase 27 remains OPEN until real production infrastructure and live acceptance
evidence pass. Local success must never be relabeled as production success.
