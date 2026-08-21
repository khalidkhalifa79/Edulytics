# Phase 25 — Local Multi-Instance Qualification

Run:

```bash
bash scripts/verify-phase25-multi-instance.sh
```

The disposable Docker topology contains PostgreSQL, Redis, two Web-role
Edulytics processes, two Worker-role Edulytics processes, and an Nginx gateway.

The gate proves shared authentication cookies, distributed Login quota,
real Redis-backed SignalR invalidation, process-once Outbox handling, worker
failover, web restart, and gateway redistribution.

The local application DB budget is `20 × 4 = 80` pooled connections. This does
not assert that the staging Neon plan supports 80 connections; provider capacity
must be verified before staging is scaled.

Expected final marker:

```text
PHASE25_MULTI_INSTANCE_LOCAL_PASS
```

## Phase acceptance boundary

This gate proves multi-process correctness. It does not claim Phase 26
performance capacity and it does not claim that the current free Render or
Neon plans can sustain the local `20 × 4 = 80` connection budget.

Paid production-like infrastructure is intentionally deferred until the
product is feature-complete and Phase 26 begins.
