# Edulytics — Phase 14 Backend Concurrency, Idempotency, Timeout and Backpressure

## Baseline

`7f909c79ac4dc92bf47ecc9401a1b03395188978`

## Goal

Guarantee deterministic behavior under simultaneous requests and bounded
behavior under overload, while preserving the Phase 13 PostgreSQL/Npgsql/Neon
architecture.

## Decisions

- ASP.NET Core request-timeout middleware supplies named timeout policies.
- ASP.NET Core rate-limiting middleware supplies bounded concurrency queues.
- Existing fixed-window security policies remain and gain explicit Retry-After
  behavior on rejection.
- Npgsql command timeout and pool budget are explicit configuration.
- Kestrel request-body/header/keep-alive limits are explicit and conservative.
- Existing optimistic RowVersion behavior remains the same-row edit authority.
- Browser form retries use a durable database idempotency reservation.
- A client may supply `Idempotency-Key`; Edulytics browser forms also generate
  `_idempotencyKey`. The antiforgery request token is a hashed fallback for
  older rendered forms, so the raw antiforgery token is never persisted.
- Duplicate same-key requests return a safe HTTP 409 instead of executing the
  business mutation twice.
- Reusing a key for a different request also returns HTTP 409.
- If response delivery is ambiguous after a reservation, the reservation is
  marked Indeterminate and retries remain fail-closed.
- Import upload keeps its existing file-hash idempotency and is excluded from
  generic form idempotency to avoid buffering a 5 MB multipart body twice.
- In-memory limiters are per process. Distributed security quotas remain a
  future multi-instance requirement and are not falsely claimed here.

## Named timeout policies

- InteractiveRead
- InteractiveWrite
- Import
- Analytics
- Operational

## Named concurrency policies

- HeavyWriteConcurrency
- ImportConcurrency
- AnalyticsConcurrency
- OperationalConcurrency

## Database

Adds durable `IdempotencyRecords` with a unique constraint over:

`ActorUserId + Operation + IdempotencyKey`

No request payload, password, token, file content or personal-data body is
stored in the idempotency table. Only a SHA-256 request hash is persisted.

## Acceptance

Phase 14 closes only after:

- clean build and full regression;
- fresh PostgreSQL migration;
- durable duplicate-key uniqueness proof;
- real PostgreSQL optimistic concurrency proof;
- real DB command-timeout proof;
- same-key duplicate execution suppression proof;
- different-key independent execution proof;
- key-reuse-with-different-payload conflict proof;
- cancellation/ambiguous-response fail-closed proof;
- concurrency queue saturation proof;
- 409 and 429 contracts verified;
- request timeout and cancellation contract verified;
- body/Kestrel limits verified;
- secret/dependency/provider audits pass;
- final commit/push and clean `HEAD == origin/main`.
