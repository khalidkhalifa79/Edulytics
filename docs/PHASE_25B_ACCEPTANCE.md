# Edulytics — Phase 25B Acceptance

## Scope

Phase 25B implements school customer acquisition and controlled onboarding:

- public Request Demo;
- persistent lead pipeline;
- SuperAdmin qualification workflow;
- 7-day temporary demo access;
- demo extend / expire / revoke;
- synthetic demo-school provisioning;
- initial SchoolAdmin provisioning;
- controlled conversion/provisioning of a real school;
- commercial activation boundary preserved for Phase 25D;
- EN/PL localization;
- anti-forgery;
- local and distributed request-demo throttling;
- audit coverage;
- application-managed RowVersion concurrency;
- PostgreSQL migration.

Phase 25B does not implement:

- public student/teacher registration;
- subscription entitlements;
- invoices;
- payment reconciliation;
- card gateways;
- automatic first-payment activation;
- Phase 26 load qualification.

## Commercial onboarding boundary

A production customer school is provisioned as `SchoolStatus.Suspended`.

Commercial activation remains deferred until the first required payment is
confirmed in the later billing/payment phase.

## Database

Migration:

`20260821225207_Phase25BCustomerOnboarding`

The migration is additive and creates:

- `DemoRequests`
- `DemoAccesses`

Both application-managed `RowVersion` properties are mapped to PostgreSQL
`bytea`.

## Local acceptance results

- CI-style build: 0 warnings / 0 errors
- Phase25B targeted tests: 14 / 14 PASS
- full regression: 338 / 338 PASS
- EN/default ↔ PL resource parity: PASS
- architecture gate: PASS
- tenant / IDOR gate: PASS
- dependency vulnerability gate: PASS
- Phase23 security/accessibility gate: PASS
- EF pending-model-change check: PASS
- Render configuration unchanged: PASS
- Phase25 scale topology unchanged: PASS
- protected main unchanged: PASS

## Delivery

Delivery must use:

`feature/phase25b-customer-onboarding -> PR -> required CI -> protected main`

Phase 25B is not considered fully closed until protected delivery and staging
browser acceptance are complete.
