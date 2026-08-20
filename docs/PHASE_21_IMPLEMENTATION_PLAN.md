# Edulytics — Phase 21 Notifications and Connector Delivery

## Baseline

`1c935c642a7562f524cdec31f1a58bbbfdf3c8da`

## Delivered scope

- durable user notification inbox;
- read / unread state;
- notification deduplication;
- durable email delivery jobs;
- observable Pending / Sent / Failed status;
- invitation email moved out of the HTTP request path;
- Outbox v2 background delivery;
- existing exponential retry / jitter / dead-letter semantics;
- finite SMTP connector timeout;
- SMTP connector circuit breaker;
- delivery success/failure audit;
- invitation queue audit;
- read/unread audit;
- EN / PL notification UI.

## Sensitive-token invariant

Password setup tokens are generated only by the background delivery
processor immediately before connector invocation.

Tokens are not fields on:

- UserNotification;
- NotificationDeliveryJob;
- OutboxMessage payload;
- AuditLog payload.

The original web request may transiently create an ASP.NET Identity token
because of the Phase 05 contract, but the durable facade discards it and
persists only the recipient user identifier plus safe origin metadata.

A fresh token is generated at actual background-delivery time.

## Connector semantics

The SMTP connector has a finite per-attempt timeout and circuit breaker.

Retry is intentionally performed by durable Outbox v2 rather than an
opaque in-memory SMTP retry loop. This keeps attempts observable and
preserves the existing exponential-backoff / jitter / dead-letter model.

SMTP delivery is inherently at-least-once at the network boundary:
a timeout after provider acceptance can lead to a later retry. This is
explicitly accepted and observable through delivery attempt state.

## Deduplication

Inbox notification:

`account-invitation:<recipient-user-id>`

Initial delivery:

`password-setup:<recipient-user-id>:initial`

Resend delivery requests use a minute-scoped key. The existing HTTP
idempotency middleware additionally protects exact retried POST requests.

## Phase boundary

Phase 22 owns the operational console for viewing/requeueing dead letters
and degraded connector state.

Phase 23 owns the broader AllowedHosts/CSP/privacy/retention hardening.

## Final acceptance

Local implementation must pass build, unit/integration, PostgreSQL,
migration, localization, architecture, tenant/IDOR and security gates.

Protected GitHub CI must pass before merge.

Final staging acceptance requires:

- Phase21 migration;
- health/readiness;
- notification route;
- real invitation queue;
- background SMTP delivery;
- delivery status Sent;
- inbox read/unread;
- audit evidence;
- no token content in Outbox/Audit/notification persistence.
