# Edulytics — Oracle Production Handoff

## Trigger

Do not begin Oracle production provisioning until:

- all planned application phases before go-live are complete;
- all required regression/security/performance/acceptance tests are green;
- the repository is on an accepted protected-main SHA;
- the user explicitly approves beginning the Oracle subscription/go-live plan.

## Final domain

The final application domain is:

```text
https://edulytiks.com
```

Do not substitute `app.edulytiks.com` as the final application domain unless a
future explicit architecture decision changes this file.

## No-cost development rule

Before the trigger above:

- retain Render Free staging;
- retain the current Neon validation environment;
- do not create a paid Render production service;
- do not purchase/upgrade hosting merely to satisfy a phase label;
- do not change final-production DNS for an unfinished application.

## Oracle planning gates

Before any paid Oracle provisioning:

1. Define the Oracle service/topology and monthly cost.
2. Confirm target region and latency to the selected database.
3. Decide whether the database remains on Neon or moves as part of the Oracle
   architecture; do not assume either outcome in advance.
4. Re-baseline CPU, memory, DB RTT, connection budget and concurrency.
5. Define TLS/ingress/reverse-proxy architecture for `edulytiks.com`.
6. Define secret storage and rotation.
7. Define persistent Data Protection key/certificate handling.
8. Define controlled migration execution.
9. Define backup/recovery, retention, RPO and RTO.
10. Define monitoring, alerting and incident ownership.
11. Define immutable-image promotion and application rollback.
12. Re-run production smoke, auth, tenant, SignalR, Outbox, audit, report,
    connector/email and performance acceptance on Oracle.

## Production acceptance

Oracle go-live is accepted only after the final immutable release passes the
production gates on the real Oracle topology.

Phase 28 post-launch DR/security/capacity review starts only after that real
go-live. It must not be inferred from Phase 27 free-environment readiness.
