# Edulytics — Phase 25D — Billing, Invoices & Bank Transfer

Phase 25D is the final commercial sub-phase. It implements school billing
profiles, invoices, manual bank-transfer recording/verification, exact daily
seat-increase proration, payment-driven initial activation and paid renewal.

Launch-v1 rules preserved from Phase 25A:

- minimum committed/billable seats = 500;
- 3/6/10-month prices = 20/15/10 per student/month;
- Poland invoice currency = PLN;
- UAE invoice/commercial currency = AED, with settlement data stored separately;
- monthly instalments or full-term upfront;
- bank transfer only; no card gateway/provider webhook;
- 14 calendar-day invoice term + 7 additional grace days;
- grace expiry creates suspension eligibility only; suspension stays manual;
- first required confirmed payment activates a pending subscription unless a
  SuperAdmin explicitly records an agreed activation date/reason;
- immediate seat increases are prorated exactly by calendar day;
- monthly cadence bills only the current partial month because later monthly
  invoices use the new commitment;
- full-term-upfront cadence bills the current partial month plus every remaining
  full anchored contract month;
- seat reductions remain renewal-only;
- tax/VAT/KSeF is data/configuration-driven and is not guessed in code.

Architecture remains Core / Services / Data / Web. Controllers do not use
DbContext or SQL. Financial writes use platform authorization, anti-forgery,
RowVersion, PostgreSQL FOR UPDATE where required, application transactions and
queued audit events. One additive PostgreSQL migration is allowed.

Delivery is one feature branch -> protected PR -> required CI -> protected main
-> Render Free staging -> live acceptance. Phase 26 remains out of scope.
