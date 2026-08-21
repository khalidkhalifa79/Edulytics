# Edulytics — Phase 25B — Registration & Customer Onboarding

## Boundary

Phase 25B implements school customer acquisition and controlled onboarding.
It does not implement subscription entitlements, invoice generation, payment
reconciliation, card payments, automatic commercial activation, or Phase 26.

## Launch flow

Public visitor:

`Request Demo -> persistent lead -> SuperAdmin qualification`

Lead pipeline:

`New -> Contacted -> DemoScheduled -> DemoCompleted -> Qualified -> Won`

Any non-terminal stage may be marked `Lost`.

Qualified leads may receive one explicit 7-day temporary demo. The demo uses a
synthetic demo school and an initial SchoolAdmin created through ASP.NET
Identity. SuperAdmin can extend, expire, or revoke it. Expiry/revocation is
checked by the existing sign-in decision path.

A Won lead can be provisioned as a real customer. The real school is created or
converted as `SchoolStatus.Suspended`, because Phase25A requires first-payment
confirmation before commercial activation. Phase25D owns that activation.

There is no public student, teacher, or SchoolAdmin registration surface.

## Security

- Request Demo is anonymous but anti-forgery protected.
- Request Demo is limited to 5 submissions/hour/IP locally and through the
  Phase25 Redis-distributed sensitive limiter.
- Platform onboarding requires `PlatformAdministration`.
- Controllers do not use DbContext.
- No passwords, payment data, or tokens are collected by the public form.
- Audit data excludes setup tokens and raw public-message contents.

## Persistence

New PostgreSQL tables:

- `DemoRequests`
- `DemoAccesses`

Both use the existing application-managed RowVersion concurrency contract.

## Acceptance

- targeted Phase25B tests;
- full regression;
- localization parity;
- architecture gate;
- tenant/IDOR gate;
- dependency gate;
- Phase23 security gate;
- EF model/migration consistency;
- no Render or Phase25 topology mutation;
- no commit/push/PR from this runner.
