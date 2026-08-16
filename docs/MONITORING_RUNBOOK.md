# Edulytics Monitoring Runbook

## Primary signals

Monitor:

- /health/live;
- /health/ready;
- HTTP 5xx responses;
- HTTP 429 responses;
- request duration;
- unhandled exception events;
- database readiness failures;
- Outbox retry warnings;
- Outbox polling failures;
- application restarts;
- SQL Server availability;
- backup success;
- restore rehearsal success.

## Liveness failure

If /health/live is not HTTP 200:

1. Check the application process.
2. Check host/container state.
3. Preserve relevant logs.
4. Restart only after useful incident evidence is retained.

## Readiness failure

If liveness is Healthy but readiness is Unhealthy, investigate:

- SQL connectivity;
- pending EF migrations;
- Outbox worker startup;
- stale Outbox heartbeat.

An Unhealthy instance should not receive normal production traffic.

## Correlation IDs

Production error pages provide a correlation ID.

Support should use that identifier when searching structured logs.

Do not request passwords, authentication cookies, invitation tokens,
database credentials, or SMTP credentials from users.

## Alert baseline

Initial operational alerting should include:

- liveness failure;
- readiness failure lasting more than two minutes;
- repeated HTTP 5xx responses;
- repeated Outbox processing failures;
- SQL Server unavailability;
- failed scheduled backup;
- missing expected backup;
- failed restore rehearsal.

Thresholds must later be tuned using real production traffic.

## Incident response

1. Record UTC incident start time.
2. Record deployed application version.
3. Capture correlation IDs.
4. Preserve structured logs.
5. Check liveness.
6. Check readiness.
7. Check SQL Server.
8. Check migration state.
9. Check Outbox processing.
10. Contain impact.
11. Restore service.
12. Validate data integrity.
13. Document root cause and corrective action.
