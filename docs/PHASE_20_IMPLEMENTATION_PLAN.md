# Edulytics — Phase 20 Reports & Exports

## Baseline

`12062cc2cf0fe4c98a377b5b59114a73afdb753a`

## Product contract

Phase 20 adds:

- School overview report for SchoolAdmin.
- Class mastery report.
- Subject mastery report.
- Student mastery report.
- Learning-outcome mastery report.
- HTML/print rendering.
- CSV export.
- XLSX export.
- durable background export jobs through Outbox v2.
- bounded HTML/export row limits and file-size limit.
- formula-injection protection.
- export request/completion/failure/download audit.
- teacher assignment scope.
- SubjectSupervisor subject scope.
- EN/PL resources.
- report timeout, concurrency and rate-limit policies.

## Authorization invariant

SchoolId is resolved from the authenticated actor.

Browser supplied tenant identifiers are never accepted.

Teachers use the same academic-year/class/subject assignment boundary as
analytics.

Subject supervisors are limited to active assigned subjects.

Only SchoolAdmin may produce the school-wide overview report.

## Export architecture

Export request:

`HTTP POST -> validation -> ReportExportJob + Outbox + Audit -> one DB save`

Background generation:

`Outbox v2 claim -> role scope revalidation -> report DTO -> CSV/XLSX -> bounded bytea -> audit`

Download:

`current actor + current scope revalidation -> owner-only file -> audit`

This means export generation survives normal browser response loss and
remains compatible with the existing durable Outbox worker.

## Spreadsheet safety

Text beginning with Excel formula prefixes is sanitized.

XLSX uses inline-string cells, never formula cells, for text data.

Numeric report values remain numeric cells.

## Memory and retention bounds

Defaults:

- HTML preview: 500 rows.
- file export: 25,000 rows.
- generated artifact: 10 MiB.
- expiry: 24 hours.
- recent jobs shown: 20.

These are safety bounds, not final capacity SLOs. Phase 26 will load-test
and tune production limits.

## PDF decision

Phase 20 intentionally implements print-friendly HTML rather than adding
an unproven PDF dependency. PDF remains optional under the master plan.

## Final acceptance boundary

Local implementation and PostgreSQL validation happen before PR.

Protected GitHub CI must pass before merge.

Final Phase 20 acceptance additionally requires Render/Neon staging
migration, health, protected route, EN/PL browser review and real
CSV/XLSX download validation without fabricated production data.
