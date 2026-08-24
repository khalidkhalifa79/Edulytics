# Edulytics Production Monitoring Runbook

## Primary signals

Monitor:

- `/health/live`;
- `/health/ready`;
- HTTP 5xx responses;
- HTTP 429 / controlled shedding;
- request p50/p95/p99 latency;
- request timeout/cancellation counts;
- application/container restarts;
- PostgreSQL/Neon connectivity and latency;
- Npgsql pool pressure / connection utilization;
- Outbox pending, processing and dead-letter counts;
- oldest pending Outbox age;
- Outbox lease/retry failures;
- SignalR connection/publish failures;
- SMTP connector failures, timeouts and circuit state;
- backup/recovery evidence;
- release SHA and migration version.

## Liveness failure

If `/health/live` is not HTTP 200:

1. Check the Render service/container state.
2. Preserve logs before destructive restart actions.
3. Record the active release SHA.
4. Check recent deploy/restart events.
5. Restore service only after useful incident evidence is retained.

## Readiness failure

If liveness is healthy but readiness is unhealthy, investigate:

- Neon/PostgreSQL reachability;
- pending or incompatible EF migrations;
- Outbox worker heartbeat/state for a Combined runtime;
- required production configuration;
- DB saturation or repeated timeouts.

An unhealthy instance must not receive normal production traffic.

## Correlation and release evidence

Search structured logs with:

- correlation ID;
- release SHA;
- SchoolId where safe;
- actor identifier where safe;
- route/operation;
- Outbox/job identifier when applicable.

Never request or log passwords, authentication cookies, invitation tokens,
database credentials, SMTP credentials, or Data Protection certificate secrets.

## Minimum production alerts

Before Phase 27 can close, alerts must exist and be testable for at least:

- sustained readiness failure;
- repeated/unexpected HTTP 5xx responses;
- repeated Outbox processing failure or dead-letter growth;
- PostgreSQL/Neon unavailability;
- material DB latency/connection pressure;
- repeated SMTP connector failure;
- unexpected application restart/deploy failure;
- missing backup/recovery capability.

Tune thresholds after observing real production traffic; do not remove the
baseline alert classes.

## Incident response

1. Record UTC incident start.
2. Record release SHA and migration version.
3. Capture correlation IDs.
4. Preserve structured logs and platform deploy events.
5. Check liveness/readiness.
6. Check Neon/PostgreSQL.
7. Check migration state.
8. Check Outbox/dead letters.
9. Check SignalR and connector degradation.
10. Contain impact.
11. Roll back the application only when schema compatibility permits it.
12. Use the backup/restore runbook for data recovery.
13. Verify tenant isolation and data integrity.
14. Document root cause and corrective action.
