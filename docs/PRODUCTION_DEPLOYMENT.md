# Edulytics Deployment and Future Production Handoff

## Current approved deployment policy

During application development and final pre-production qualification:

```text
Render Free staging
+
current Neon Edulytics environment
```

No paid Render production service is required or approved at this stage.

The existing staging URL remains:

```text
https://staging.edulytiks.com
```

The final production domain is reserved as:

```text
https://edulytiks.com
```

Do not perform the final DNS/application cutover until the later Oracle
production plan is explicitly started.

## Immutable delivery

Protected GitHub CI builds/tests the exact release SHA. Immutable SHA-tagged
container artifacts remain the promotion unit.

This rule is retained even though the actual paid production host is deferred.

## Database connection contract

Application runtime configuration:

```text
ConnectionStrings__DefaultConnection
```

Migration/admin configuration:

```text
ConnectionStrings__MigrationConnection
```

Keep runtime and migration credentials separate.

The current PostgreSQL/Neon implementation remains the accepted application
baseline. The final database placement during Oracle go-live must be explicitly
decided and revalidated; this document does not silently assume a future move or
non-move.

## Migration ordering contract

Normal production-style startup defaults to **no automatic migration**.

A controlled release must run the migration bundle separately before serving
traffic. The existing container helper remains:

```text
/app/phase27-predeploy.sh
```

The Render Free staging compatibility flag may remain enabled only for the
temporary staging topology:

```text
Edulytics__Deployment__RunStartupMigrations=true
```

The future real production topology must keep startup migration disabled and use
a controlled migration/release step.

## Current acceptance surface

Free-environment qualification verifies:

```text
GET https://staging.edulytiks.com/health/live
GET https://staging.edulytiks.com/health/ready
```

It also verifies security headers and EN/PL entry flow.

Authenticated tenant, concurrency, SignalR, Outbox and long-duration performance
evidence is inherited from the accepted test suites and Phase 26 qualification
unless a material runtime change invalidates that evidence.

## Future Oracle production

Actual production provisioning/cutover is governed by:

`docs/ORACLE_PRODUCTION_HANDOFF.md`

That later plan must revalidate capacity and performance on Oracle before
customer production acceptance.

## Explicit non-goals now

Current Phase 27 work does not:

- create or upgrade a paid Render service;
- purchase Oracle resources;
- perform final `edulytiks.com` DNS cutover;
- claim production customer traffic is live;
- start Phase 28.
