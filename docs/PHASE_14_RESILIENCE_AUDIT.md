# Phase 14 Resilience Audit

## Cancellation

Existing asynchronous MVC data paths were reviewed. Controllers already pass
`CancellationToken` through the service layer in the implemented school,
academic, curriculum, assessment, analytics and import workflows. Phase 14
adds request-timeout middleware so timeout cancellation joins the existing
request cancellation token path.

## Rate/concurrency protection

Existing security throttles are preserved:

- SchoolUserCreate;
- InvitationResend;
- PasswordSetup.

New bounded concurrency policies protect:

- assessment writes;
- import upload/confirm;
- analytics recalculation;
- future operational actions.

Rejected requests return 429 and Retry-After. A concurrency limiter cannot
predict an exact permit-availability time, so Edulytics emits a conservative
`Retry-After: 1` when limiter metadata has no estimate.

## Idempotency coverage

Authenticated business form mutations are protected when an explicit
idempotency key or antiforgery-backed browser form key is present. All current
Edulytics state-changing Razor forms carry antiforgery protection; the app
layout also injects an explicit per-render idempotency key into POST forms.

Import upload remains governed by its existing SHA-256 file-hash idempotency to
avoid double-buffering multipart payloads.

Same-row edits remain protected by application-managed `RowVersion` optimistic
concurrency established and proven against PostgreSQL in Phase 13.

## Database budget

Defaults are intentionally conservative and configurable:

- Npgsql maximum pool size: 40 per app process unless connection string
  explicitly overrides it;
- database command timeout: 15 seconds;
- import concurrency: 2 active + 2 queued;
- analytics recalculation: 1 active + 1 queued;
- heavy writes: 8 active + 8 queued.

These values are safety defaults, not final production capacity claims.
Measured tuning belongs to production-like load/stress qualification.

## Multi-instance boundary

ASP.NET Core in-memory rate/concurrency limiters are per process. Phase 14 does
not claim global quotas across multiple instances. Distributed security quotas,
Outbox multi-worker ownership and horizontal-scale qualification remain later
master-plan work.

## Kestrel review

Phase 14 explicitly bounds request body size, request-header time and
keep-alive time. It intentionally does not cap upgraded WebSocket connections
before SignalR load measurements, because an arbitrary low value could break
valid realtime traffic.
