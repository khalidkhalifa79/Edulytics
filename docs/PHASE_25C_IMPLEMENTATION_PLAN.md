# Edulytics — Phase 25C Subscriptions, Entitlements, Seats & Renewals

## Source contract

Phase 25C implements the accepted `PHASE_25A_COMMERCIAL_MODEL.md` without
introducing Phase 25D invoice/payment reconciliation.

Launch v1 rules implemented by this phase:

- school is the commercial tenant;
- only student seats are commercially counted;
- minimum committed seats = 500;
- active student profiles consume seats;
- archived/non-active commercial student profiles do not consume active seats;
- 3-month plan = 20 per student/month;
- 6-month plan = 15 per student/month;
- 10-month school-year plan = 10 per student/month;
- Poland commercial currency = PLN;
- UAE commercial currency = AED;
- monthly instalments or full-term upfront are contract cadences, not
  month-to-month cancellation;
- seat increases take effect immediately and are timestamped so Phase 25D can
  calculate daily proration;
- seat reductions are scheduled for renewal only;
- renewal may not go below 500 or below the current active-student count;
- auto-renew is optional;
- disabling auto-renew for an active term requires at least 30 days' notice;
- activation timestamp and current-term boundaries are durable and audited;
- suspension/reactivation does not delete school data;
- no automatic late-payment suspension is added here.

## Current student-status compatibility

The current codebase models `StudentProfile.Status` with
`AcademicStructureStatus.Active/Inactive`; it does not yet contain a dedicated
student `Archived` lifecycle. Core Phase 25C therefore counts only the existing
`Active` status as consuming current seat capacity. The enforcement/UI step must
make the student archival semantic explicit rather than silently inventing a
new meaning for unrelated academic-structure statuses.

## Domain model

`SchoolSubscription`
- one row per commercially managed school;
- plan/term;
- billing cadence;
- commercial currency;
- fixed launch unit price;
- committed seats;
- optional renewal seat target;
- auto-renew/non-renewal state;
- PendingActivation / Active / Suspended / Ended lifecycle;
- first activation timestamp;
- current term start/end;
- optimistic `RowVersion`.

`SubscriptionSeatChange`
- tenant-safe append-style commercial history;
- Initial / Increase / RenewalAdjustment;
- previous and new committed seats;
- exact effective timestamp;
- retained for later Phase 25D proration/invoice calculation.

## Entitlement semantics

All launch plans contain the same product features, so Phase 25C does not invent
feature tiers.

Entitlement evaluation determines:
- whether the school is commercially managed;
- whether operational access is currently allowed;
- current active-student count;
- committed seat ceiling;
- available active-student capacity.

Existing demo/legacy schools without a subscription remain outside this
commercial entitlement check until explicitly converted.

## Activation boundary

Phase 25C exposes domain activation/reactivation operations and keeps school
status synchronized with subscription status.

Phase 25D will call the activation/reactivation domain operation after the first
required bank-transfer payment is confirmed. Phase 25C itself contains no
invoice, payment, bank-reconciliation or card-provider implementation.

## Delivery sequence

1. Core subscription domain + persistence + commercial policy.
2. Domain service + audit + concurrency + migration.
3. Wire operational entitlement checks into sign-in/SchoolAccess.
4. Wire seat-cap enforcement into direct active-student creation and confirmed
   student imports with a concurrency-safe subscription-row lock.
5. Add explicit student archive/restore semantics needed by the seat contract.
6. Add SuperAdmin subscription management UI and EN/PL localization.
7. Full PostgreSQL, security, tenant and browser acceptance.
8. Protected PR/CI/merge/staging acceptance.

This file is implementation planning, not Phase 25C closeout.
