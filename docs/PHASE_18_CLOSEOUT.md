# Phase 18 — Audit & Compliance Foundation Closeout

Date: 2026-08-18

## Implemented

- Durable AuditLog entity/table and additive migration.
- Actor, role, school, action, entity, timestamp, correlation ID, IP, User-Agent, source and feature metadata.
- Safe old/new summaries with centralized sensitive-value sanitization.
- Normal application append-only enforcement for AuditLog.
- Transaction-safe audit persistence across:
  - schools;
  - school users and Identity/UserManager operations;
  - active/lock/role changes;
  - password invitation issue/completion metadata;
  - academic structure and enrollments;
  - curriculum and curriculum adoption;
  - assessments, question mappings and results;
  - imports;
  - manual Outbox dead-letter requeue.
- Existing OutboxRequeueAudit retained.
- Audit query repository and query service.
- SchoolAdmin own-school visibility enforcement.
- SuperAdmin all-school/platform visibility.
- Cross-school IDOR policy.
- Filters for school, action, entity, correlation ID, actor and UTC range.
- Pagination.
- English and Polish Audit Viewer.
- PostgreSQL integration coverage for:
  - migration and durable insert;
  - correlation search;
  - tenant isolation;
  - append-only enforcement;
  - rollback/no false-success audit;
  - Outbox dual-audit requeue.

## Local validation

- Full solution build: PASS
- Full regression: PASS
- Static audit/security acceptance: PASS
- PostgreSQL local gate: PASS (local PostgreSQL 17)

The PostgreSQL gate is also part of the protected pull-request CI path and
must pass before merge.

## Sensitive data contract

Audit payloads must not store passwords, password hashes, reset/setup tokens,
API keys, SMTP credentials, database credentials, cookies, authorization
headers, Data Protection secrets, raw uploaded file bytes, raw import rows,
question prompts, or detailed student answer payloads.

## Acceptance rule

Phase 18 is accepted only after required PR CI passes and the protected-branch
merge succeeds.
