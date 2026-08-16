# Edulytics — Phase 15 Outbox v2, Analytics Coalescing and Realtime Consistency

## Baseline

`94a3fea9f2d9450d66f125587bb6cd5ef83b0528`

## Invariants

1. PostgreSQL is the source of truth.
2. An Outbox message has exactly one active claim owner/token.
3. Fetch and claim happen in one PostgreSQL transaction using row locks and
   `SKIP LOCKED`.
4. Completion/failure requires the active owner + immutable lease token.
5. Expired leases can be reclaimed; stale workers cannot complete them.
6. Poison messages stop after a finite attempt count and become DeadLetter.
7. Retry uses bounded exponential backoff with jitter.
8. Dead-letter requeue is an atomic audited operation requiring actor + reason.
9. Outbox processing no longer performs full analytics recalculation per event.
10. Result/import events mark one durable analytics-refresh state dirty by
    SchoolId.
11. Analytics refresh has one database-backed lease per school, so only one
    expensive school refresh runs at a time.
12. Burst events coalesce using a debounce window with a bounded maximum wait.
13. SignalR is only an invalidation hint. Browser data is reconciled by an
    authoritative page reload.
14. Duplicate/out-of-order SignalR messages are harmless.
15. Reconnect always schedules an authoritative refresh.
16. Background work is bounded to finish comfortably inside its lease, giving
    safe graceful shutdown behavior without an unbounded in-flight task.

## Outbox state

`Pending -> Processing -> Processed`

Repeated failure:

`Processing -> Pending -> ... -> DeadLetter`

Claim fields:

- LeaseOwner
- LeaseToken
- LeaseUntilUtc

Operational recovery:

- dead-letter listing contract;
- audited requeue with ActorUserId + Reason + PreviousAttempts.

## Analytics coalescing

One `AnalyticsRefreshState` row per SchoolId stores:

- RequestedVersion;
- CompletedVersion;
- first/last request times;
- coalesce deadline;
- next availability;
- lease owner/token/deadline;
- failure metadata.

Multiple result/import events increment RequestedVersion on one row.
One worker claims one school. Events arriving during a refresh advance
RequestedVersion and cause one later refresh instead of parallel refreshes.

## Realtime

All authorized analytics users join one school-level invalidation group in
addition to their existing scoped groups.

After a successful analytics refresh:

`AnalyticsUpdated`

means only:

`state changed; fetch latest authoritative state`.

The browser debounces event bursts and refreshes on reconnect.

## Phase boundary

Phase 15 does not add a SignalR backplane, shared Data Protection, a full
operator UI, or multi-instance load qualification. Those are later master-plan
phases. Phase 15 makes the queue/refresh ownership model safe before that scale
is enabled.

## Acceptance

- Phase15 tests PASS;
- full regression PASS;
- fresh PostgreSQL migration PASS;
- atomic two-worker claim PASS;
- stale-owner completion rejection PASS;
- expired-lease reclaim PASS;
- dead-letter PASS;
- audited requeue PASS;
- jitter bounds PASS;
- per-school fairness PASS;
- analytics coalescing PASS;
- analytics single-flight PASS;
- analytics stale-owner rejection PASS;
- Production-mode runtime smoke PASS;
- reconnect/debounce/static realtime contract PASS;
- dependency/provider/secret audits PASS;
- commit/push PASS;
- clean `HEAD == origin/main`.
