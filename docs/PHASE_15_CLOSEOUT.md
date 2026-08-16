# Edulytics — Phase 15 Closeout

## Baseline

`94a3fea9f2d9450d66f125587bb6cd5ef83b0528`

## Accepted Outbox v2 contract

- PostgreSQL claim is one transaction using `FOR UPDATE SKIP LOCKED`.
- Claim returns immutable owner/token.
- stale completion/failure is rejected.
- expired leases are reclaimable.
- processing is bounded below lease duration.
- max attempts are finite.
- poison events become DeadLetter.
- retry delay is exponential, capped and jittered.
- dead letters can be listed through a repository contract.
- manual requeue requires ActorUserId + reason and writes an audit row.
- each claim batch takes at most one oldest eligible event per SchoolId,
  preventing one tenant from filling an entire batch.

## Analytics

- Outbox processing only validates the durable event and marks one school
  analytics state dirty.
- one durable `AnalyticsRefreshState` exists per SchoolId.
- bursts increment a requested version and debounce to one refresh window.
- maximum coalesce delay prevents indefinite starvation during a continuous
  burst.
- one active database lease per SchoolId provides distributed single-flight.
- events arriving during a refresh remain dirty and trigger one follow-up
  refresh.
- stale analytics lease completion is rejected.

## Realtime

- PostgreSQL remains authoritative.
- SignalR sends `AnalyticsUpdated` as an invalidation hint after a successful
  refresh.
- all authorized analytics users join a tenant-scoped school invalidation
  group.
- browser invalidations are debounced.
- reconnect schedules authoritative page reconciliation.
- duplicate/out-of-order messages never apply business state in JavaScript.

## Graceful shutdown

- workers stop claiming when the host stopping token is set.
- an already claimed unit uses its own bounded processing timeout.
- message processing timeout is configured below Outbox lease duration.
- analytics refresh timeout is configured below analytics lease duration.
- host shutdown grace is bounded.
- if the process dies, the lease expires and another worker reclaims safely.

## Evidence

- Phase15 automated tests PASS.
- full regression PASS.
- fresh PostgreSQL migration PASS.
- two-worker single-claim business-effect proof PASS.
- fairness proof PASS.
- Outbox crash/reclaim PASS.
- stale Outbox completion rejection PASS.
- dead-letter + audited requeue PASS.
- analytics 20-event coalescing PASS.
- analytics distributed single-flight PASS.
- events-during-refresh follow-up PASS.
- analytics stale-owner rejection PASS.
- Production-mode startup/readiness/graceful-shutdown smoke PASS.
- dependency/provider/secret audits PASS.

## Deferred by master-plan design

The following are intentionally not falsely claimed by Phase 15:

- SignalR Redis/managed backplane;
- shared Data Protection keys;
- distributed global security rate limits;
- dedicated worker service;
- production multi-instance load/soak qualification;
- full operator web console.

Those are handled by later phases before horizontal production scale is
accepted.
