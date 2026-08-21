# Edulytics — Phase 25A Commercial Model & Pricing

## Status

**Accepted / closed before Phase 25B implementation.**

This document records the commercial decisions that Phase 25B–25D must implement.
It is a product/business contract, not a claim that billing or onboarding code
already exists.

## Customer model

Launch v1 customer type:

- School only.
- School is the Edulytics tenant/customer.
- SchoolAdmin, SubjectSupervisor, Teacher and Student are users inside that
  school.
- Teachers, SchoolAdmins and SubjectSupervisors are not separately billed.
- Commercial billing is based on licensed student seats only.
- Architecture may remain extensible for future IndependentTeacher /
  IndependentStudent customer types, but those customer types are not exposed
  in launch v1.

## Student-seat contract

- Minimum committed/billable student seats: **500**.
- An `Active` student consumes a seat.
- An `Archived` student does not consume an active seat.
- Inactivity/login frequency does not free a seat.
- `CommittedSeats` is the commercial floor during the active contract term.
- The school pays for `max(500, CommittedSeats)`, even when current active
  students are lower.
- Seat increases are allowed immediately.
- Seat increases use exact seat counts; there are no blocks.
- Seat increases are prorated using exact daily proration for the remainder of
  the current billing period.
- Seat reductions do not reduce the current contract commitment.
- Seat reductions become effective at renewal only.
- Renewal may not go below 500 seats.

## Contract terms and pricing

All plans contain the **same product features**. The difference is only contract
length and price.

| Commitment | Price per student per month |
| --- | ---: |
| 3-Month Plan | 20 |
| 6-Month Plan | 15 |
| Full School Year (10 Months) | 10 |

The numeric price is fixed across launch markets and expressed in the local
market currency:

- Poland: PLN.
- UAE: AED.

The 10-month term is a school-year commitment and must not be labelled as a
calendar annual plan.

## Billing cadence

The school chooses one of:

- monthly instalments; or
- full-term upfront payment.

Monthly instalments do **not** create month-to-month cancellability. The school
remains committed to the selected 3/6/10-month term unless an exceptional
termination is approved.

## Payment method

Launch v1:

- **Bank transfer only.**
- No card payment gateway in launch v1.

OUR-CS currently settles through bank accounts in:

- PLN;
- EUR.

Poland commercial pricing/invoicing is in PLN.

For UAE customers, the commercial contract price remains in AED while the
invoice/payment instructions can state the EUR settlement equivalent applicable
at invoice issuance for payment into the EUR account. The system must keep
commercial/invoice currency data separate from settlement currency and store
the actual amount/currency received.

No payment-provider/card-webhook implementation belongs in launch v1.

## Subscription activation

- Contract duration starts from the school's actual activation date.
- Initial operational activation occurs after the first required payment is
  confirmed, unless SuperAdmin intentionally sets an agreed activation date
  consistent with the commercial agreement.
- The activation timestamp is persisted and auditable.

## Invoice terms and late payment

- Invoice payment term: **14 calendar days**.
- After the due date, the account enters the late-payment/grace workflow.
- Grace period: **7 additional days**.
- After grace expiry the school becomes eligible for suspension.
- Launch v1 suspension is **not automatic**.
- SuperAdmin makes the suspension/reactivation decision and the action is
  audited.

Recommended operational flow:

`Due -> Overdue / GracePeriod -> SuspensionEligible -> Suspended`

## Suspension semantics

Suspension blocks normal operational product use by school users.

Suspension does **not** delete:

- school data;
- students;
- results;
- curriculum associations;
- audit history;
- invoices/payment history.

SuperAdmin retains administrative access. Payment confirmation/manual
reactivation can restore the school.

## Payment / invoice state vocabulary

The billing implementation should support, as applicable:

- Draft
- Pending
- Due
- PartiallyPaid
- Paid
- Overdue
- Refunded
- PartiallyRefunded
- Cancelled
- Rejected (for a rejected/mismatched manual bank-transfer verification)

`Failed` is not a primary bank-transfer state.

## Bank-transfer verification

Launch v1 uses manual verification:

1. invoice/payment instructions are issued;
2. school makes bank transfer and provides payment reference/evidence where
   required;
3. SuperAdmin verifies receipt;
4. payment is marked as paid;
5. activation/reactivation proceeds when the commercial rules permit it.

Automated bank reconciliation is a later enhancement.

## Auto-renewal / non-renewal

- Auto-renew is optional.
- The school chooses whether it wants auto-renewal.
- If enabled, non-renewal notice is required at least **30 days** before the
  current contract end date.
- If auto-renew is disabled, the contract ends at its EndDate unless a new term
  is agreed.

## Early cancellation

Monthly payment does not erase the contractual commitment.

For a 3/6/10-month term, the school remains responsible for the agreed term
unless SuperAdmin approves an exceptional termination under the applicable
commercial/legal terms.

## Refunds / credits

Initial policy:

- no refund for a monthly billing period that has already started;
- full-term prepaid refunds only under the applicable cancellation/legal terms;
- SuperAdmin may issue an audited manual adjustment/credit when required.

## Billing identity fields

The product must be able to store:

- legal school name;
- billing address;
- country;
- tax/VAT/TRN identifier as applicable;
- invoice currency;
- settlement currency where different;
- invoice email;
- invoice number;
- invoice issue date;
- due date;
- payment reference;
- bank account/payment instructions;
- actual settled amount/currency;
- payment confirmation timestamp.

OUR-CS is the Polish seller/company behind Edulytics.

Tax/KSeF/VAT treatment must be configuration/data-driven and follow the
applicable accountant/legal rules. The product must not hard-code one tax rule
for every country/customer.

## Demo policy

Launch v1:

- Demo only.
- No public free trial.
- Public demo request does not automatically create a production school tenant.
- Qualified prospects may receive temporary demo access for **7 days**.
- SuperAdmin can grant, extend, revoke or expire demo access.
- Demo data must be synthetic/non-production.
- The demo workflow must be auditable.

Suggested sales lifecycle:

- New
- Contacted
- DemoScheduled
- DemoCompleted
- Qualified
- Won
- Lost

## Public/commercial onboarding boundary

The safe launch flow is:

`Public pricing -> Request Demo -> sales qualification -> controlled demo ->
commercial agreement -> bank-transfer/payment confirmation -> controlled school
tenant + first SchoolAdmin activation`

Students must never public-register and select an arbitrary school.

## Phase boundaries

Phase 25B owns customer registration/onboarding and the demo-request/customer
provisioning workflow.

Phase 25C owns subscriptions, entitlements, committed seats, renewal and
operational subscription enforcement.

Phase 25D owns billing/invoices/bank-transfer recording and payment-driven state
transitions.

Phase 26 load/stress/spike/soak testing must not begin until the commercial
product feature set is complete.
