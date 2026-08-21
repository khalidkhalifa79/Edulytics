# Phase 25 — Multi-Instance Scale Qualification Acceptance

## Accepted baseline

`710b3c4ff796d7e16000a09f11db620b1aee84fb`

## Scope

Phase 25 proves that Edulytics does not depend incorrectly on process-local
memory when multiple Web and Worker processes are active. It is not the Phase
26 load/stress/spike/soak qualification.

## Implemented scale contracts

- explicit Combined/Web/Worker runtime roles;
- Redis-compatible SignalR scale-out, configuration-gated;
- shared PostgreSQL Data Protection persistence retained;
- distributed security-sensitive quota enforcement through Redis;
- explicit total Npgsql connection-budget validation;
- Web/Worker hosted-service role separation;
- concurrent first-start PostgreSQL bootstrap serialization;
- single-instance rollback remains the default when scale mode is disabled.

## Executable qualification topology

`scripts/verify-phase25-multi-instance.sh` creates PostgreSQL 17, Redis 7.4,
Web1, Web2, Worker1, Worker2 and an Nginx gateway.

## Accepted executable evidence

The gate proves:

- 2 Web processes and 2 Worker processes;
- Web processes do not run the Outbox workers;
- both Worker processes can independently process after peer loss;
- Login quota is global across Web1 and Web2;
- an Identity cookie issued by Web1 decrypts on Web2;
- Redis-backed SignalR invalidation crosses process boundaries;
- Outbox processing remains process-once under worker failover;
- authenticated traffic redistributes to Web2 after Web1 loss;
- the original cookie remains valid after Web1 restart;
- local DB budget is `20 × 4 = 80`.

Expected runtime marker:

```text
PHASE25_MULTI_INSTANCE_LOCAL_PASS
```

## Regression acceptance

- CI-style build;
- Phase25 targeted tests;
- full regression;
- localization parity;
- architecture gate;
- tenant/IDOR gate;
- dependency vulnerability gate;
- Phase23 security gate;
- repository secret-history gate;
- `git diff --check`.

## Cost/provider boundary

The current free Render staging service and Neon database remain unchanged
during feature development. Paid production-like infrastructure is introduced
for Phase 26 after feature completion, when final provider sizing and the
10,000-concurrent-student qualification are measured.

## Exit gate

> No behavior depends incorrectly on process-local memory.

Phase 26 is not started by this acceptance.
