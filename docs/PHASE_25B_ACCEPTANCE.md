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

## Staging corrective — public language switching

Staging browser acceptance identified that `/request-demo` correctly rendered
the persisted culture, but the public layout did not expose an in-page language
control.

The corrective adds:

- visible English / Polish controls to `_PublicLayout`;
- POST-only culture switching through the existing anti-forgery-protected
  `/set-culture` endpoint;
- safe same-page return through a validated local `returnUrl`;
- explicit open-redirect protection via `Url.IsLocalUrl`;
- responsive and keyboard-visible controls.

Phase 25B remains open until this corrective is delivered through protected CI
and verified on staging.

## Staging visual corrective — Request Demo composition

The first public-language corrective was functionally correct but staging
browser review showed that the switcher floated outside the Request Demo card,
which was not an acceptable public-facing composition.

The visual corrective:

- removes the language control from the global body-level public layout;
- places localized branding and the EN/PL segmented control inside the Request
  Demo card header;
- applies the same header treatment to the thank-you page;
- improves form field spacing, borders, focus states, consent treatment and CTA;
- preserves the existing server-side onboarding behavior, localization,
  anti-forgery and safe return URL contract;
- remains responsive at tablet and 320–420px mobile widths.

No database, billing, subscription or tenant behavior changes are included.

## Staging corrective — supported country selector

Browser acceptance showed that free-text `Country code` could be mistaken for a
phone/area code (`06` was entered during staging).

Launch v1 currently supports two commercial markets only:

- Poland (`PL`)
- United Arab Emirates (`AE`)

The corrective:

- replaces free-text country entry with a localized dropdown;
- persists ISO 3166-1 alpha-2 codes (`PL` / `AE`);
- enforces the same allow-list server-side;
- prevents arbitrary values such as `06`;
- keeps phone entry separate.

New markets must be enabled explicitly together with their commercial, billing
and tax rules.

## Staging corrective — DemoScheduled UTC persistence

Staging browser acceptance found that `Contacted -> DemoScheduled` failed with
the generic persistence error after a valid `datetime-local` value was entered.

Browser `datetime-local` values are timezone-less. The onboarding field is
explicitly defined as UTC, so the service now explicitly marks the
administrator-entered value as `DateTimeKind.Utc` before assigning
`DemoScheduledAtUtc`.

A regression test covers `DateTimeKind.Unspecified -> DateTimeKind.Utc`.

No migration or Data-layer change is required.

## Staging corrective — mandatory Phase 25B audit composition

Staging acceptance found that the completed Phase 25B onboarding operations
were not visible in the Audit log.

A semantic source inspection confirmed that the Phase 18 audit subsystem is
registered and that `AuditService.RecordAsync` persists through
`IAuditRepository.SaveChangesAsync`. It also found a Phase 25B-specific
fail-open contract: `CustomerOnboardingService` accepted `IAuditService` as an
optional dependency, `AuditAsync` silently returned when it was absent, and the
direct Phase 25B service tests intentionally constructed the service without an
audit dependency.

The corrective removes that silent path. `IAuditService` is now mandatory and
the Phase 25B production composition explicitly resolves it through
`GetRequiredService<IAuditService>()`. Regression coverage now verifies:

- Grant Demo emits `DemoAccess.Granted`;
- the Audit writer persists a real `AuditLog` through the repository;
- the production onboarding registration resolves with a mandatory audit
  dependency;
- Phase 25B direct service tests no longer bypass audit composition.

This corrective does not change the database schema, migrations, Render
topology, Phase 25 scale topology, pricing, subscription, or payment boundaries.

Historical staging operations executed before this corrective are not
fabricated or backfilled. Staging must generate one new Phase 25B auditable
operation after deployment to close the acceptance gate.
