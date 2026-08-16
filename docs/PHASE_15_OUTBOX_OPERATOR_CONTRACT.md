# Phase 15 Outbox Operator Contract

Routine recovery must not require ad-hoc SQL.

The durable repository contract exposes dead letters with:

- message ID;
- SchoolId;
- event type;
- attempts;
- last error;
- occurrence time;
- dead-letter time.

Requeue requirements:

1. target must currently be DeadLetter;
2. ActorUserId is mandatory;
3. non-empty reason is mandatory and bounded;
4. requeue and audit insert are one PostgreSQL transaction;
5. attempts reset only as part of that audited requeue;
6. lease owner/token are cleared;
7. message becomes immediately Pending.

Phase 22 will place an authorized operational UI over this contract. Phase 18
will expand business audit globally; the Phase 15 requeue audit remains durable
and is not deferred.
