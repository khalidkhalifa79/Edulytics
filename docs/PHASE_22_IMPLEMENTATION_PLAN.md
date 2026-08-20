# Edulytics — Phase 22 Operational Admin Console

## Baseline

`f6257777f8dbd9b1957c036abe06b0255637d545`

## Goal

Routine operational recovery must not require ad-hoc database manipulation.

The console is restricted to the existing `PlatformAdministration`
authorization policy.

## Delivered operational visibility

- Outbox Pending / Processing backlog;
- dead-letter listing;
- audited dead-letter requeue;
- Outbox worker started/heartbeat state;
- analytics requested/completed freshness;
- recent import validation failures;
- release SHA;
- latest applied EF migration;
- SMTP connector enabled/degraded/circuit state.

Raw Outbox payloads are deliberately not projected to the UI.

Import `RowsJson`, file hash and original file name are deliberately not
projected to the operator console.

No SMTP credentials, setup tokens or recipient addresses are exposed.

## Safe requeue invariant

Phase 15 already guarantees that Outbox requeue:

- accepts only DeadLetter state;
- requires actor and bounded reason;
- writes the operator audit and state transition in one PostgreSQL
  transaction;
- clears leases;
- resets Outbox attempts;
- makes the message immediately Pending.

Phase 22 additionally coordinates durable dependent state.

For `Notifications.DeliveryRequested`, requeue is permitted only when the
delivery job is `Failed` specifically because of `OutboxDeadLettered`; that
job is reset to Pending in the same PostgreSQL transaction.

For `Reports.ExportRequested`, requeue is permitted only when the export job
is `Failed` specifically because of `BackgroundDeliveryDeadLettered` and has
not expired; that job is reset to Pending in the same PostgreSQL transaction.

This prevents an operator from requeueing an Outbox row while leaving its
dependent durable job terminal, which would otherwise produce a false
successful recovery.

Routine requeue remains rejected for unknown event families.

## Operator audit

The existing durable Phase 15 audit remains the source of truth:

`Outbox.DeadLetterRequeued`

The operator actor ID and reason are mandatory. The transition and audit are
atomic.

## Schema impact

No migration is required.

Phase 22 uses existing:

- OutboxMessages;
- OutboxRequeueAudits;
- AuditLogs;
- AnalyticsRefreshStates;
- SchoolAnalyticsSnapshots;
- ImportBatches;
- NotificationDeliveryJobs;
- ReportExportJobs.

## Security boundary

- `/platform/operations` is `PlatformAdministration` only.
- requeue is POST + antiforgery.
- no raw event payload is rendered.
- no connector secrets are rendered.
- no manual SQL is exposed to operators.

## Phase boundary

Phase 23 owns broader CSP, AllowedHosts, privacy/retention and accessibility
hardening.

Phase 22 does not start Phase 23 work.
