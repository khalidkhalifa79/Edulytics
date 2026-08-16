# Edulytics — Phase 14 Closeout

Phase 14 implements bounded backend concurrency, named request timeouts,
durable idempotency, explicit Npgsql DB timeout/pool budgets, Kestrel request
limits and deterministic duplicate/conflict behavior.

## Baseline

`7f909c79ac4dc92bf47ecc9401a1b03395188978`

## Accepted behavior

- duplicate same-key browser/business mutation: one execution, later duplicate
  receives safe 409;
- same key with different request hash: 409 key-reuse conflict;
- different keys: independent operations;
- ambiguous cancellation/response-loss path: fail-closed Indeterminate state;
- same-row edit: PostgreSQL optimistic concurrency rejects stale writer;
- endpoint overload: bounded concurrency queue, then 429;
- 429 includes Retry-After;
- named request timeout policies are registered and applied to expensive paths;
- Npgsql command timeout is bounded;
- Npgsql pool size has an explicit per-process budget unless externally
  overridden;
- request body/header/keep-alive limits are explicit;
- import upload retains existing file-hash idempotency and 5 MB parser limit;
- SignalR is not arbitrarily constrained by an unmeasured upgraded-connection
  cap.

## Multi-instance boundary

The current rate/concurrency limiters are per-process protection. Global
security quotas, Outbox v2 ownership, shared realtime scale-out and final
multi-instance capacity qualification remain later Production Master Plan
phases.

## Evidence

- Phase 14 automated tests;
- full regression;
- clean fresh PostgreSQL migration;
- real PostgreSQL durable idempotency proof;
- real PostgreSQL stale-write conflict proof;
- real PostgreSQL DB command-timeout proof;
- bounded ConcurrencyLimiter queue-overflow test;
- dependency vulnerability audit;
- secret scan;
- PostgreSQL-only active provider audit.
