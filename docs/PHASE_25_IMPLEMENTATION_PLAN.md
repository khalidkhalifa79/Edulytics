# Phase 25 — Multi-Instance Scale Qualification

## Goal

Prove Edulytics behaves correctly with multiple web and worker instances even
if the first production rollout later chooses a single web instance.

## Master-plan required work

- 2 web instances;
- SignalR scale-out backplane or managed service;
- shared Data Protection;
- 2 workers;
- Outbox multi-worker tests;
- distributed sensitive rate limits;
- database connection budget;
- sticky-session behavior only if required by the chosen SignalR scale method;
- instance kill/restart;
- traffic redistribution;
- session/cookie continuity.

Exit gate:

> No behavior depends incorrectly on process-local memory.

## Invariants

1. PostgreSQL remains authoritative business truth.
2. Outbox ownership remains atomic and stale-owner completion is rejected.
3. SignalR remains an invalidation transport, never business truth.
4. Authentication cookies must decrypt on every web instance.
5. Security-sensitive quotas must not multiply silently with instance count.
6. Background processing must not duplicate effects when two workers run.
7. Total Npgsql pool capacity must be explicitly budgeted across instances.
8. A killed instance must not corrupt durable state.
9. Phase 25 does not perform Phase 26 load/stress/soak qualification.

## Implementation order

1. discovery and current-contract evidence;
2. runtime role separation for web and workers;
3. shared SignalR backplane integration;
4. distributed sensitive-rate-limit mechanism;
5. explicit multi-process DB pool budget validation;
6. local PostgreSQL + Redis multi-instance harness;
7. two-worker / cross-instance cookie / realtime tests;
8. execute the disposable local PostgreSQL + Redis + 2-web + 2-worker + gateway topology;
9. prove kill/restart, traffic redistribution, shared cookies, distributed quotas, SignalR scale-out and multi-worker Outbox behavior;
10. protected PR/CI/merge. The paid exact production-like performance topology is intentionally deferred to Phase 26 after product feature completion.

## Schema impact

Do not create a schema migration merely for horizontal scale.

A migration is allowed only if the selected distributed quota design requires a
durable PostgreSQL structure and repository tests prove that design preferable
to the selected shared backplane store.

## Rollback

Scale features must be configuration-gated. A single-instance deployment must
remain valid with scale-out disabled.

## Acceptance environment decision

Phase 25 is a multi-instance correctness qualification, not the Phase 26
capacity/load test. Its master-plan goal explicitly allows the initial
production rollout to remain single-instance while scale correctness is proven.

The repository-owned disposable Docker topology is therefore the executable
Phase 25 acceptance environment. The current free Render staging service and
Neon database remain unchanged while product development continues at the
zero-cost target.

Phase 26 remains responsible for the exact production-like topology,
load/stress/spike/soak qualification, final provider sizing and paid
infrastructure before go-live.
