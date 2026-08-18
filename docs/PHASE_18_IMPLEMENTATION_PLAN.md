# Edulytics — Phase 18 Audit & Compliance Foundation

## Goal
Implement durable, tenant-safe business auditing for sensitive Edulytics operations.

## Required foundation
- AuditLog durable entity/table
- repository + writer/service + query service
- actor, SchoolId, action, entity, timestamp, correlation ID, IP and user agent metadata
- safe old/new or change summaries where appropriate
- append-only normal application contract
- sensitive-data redaction
- SchoolAdmin own-school audit viewer
- explicit platform/SuperAdmin visibility
- EN/PL localization
- pagination and filters
- transaction-safe audit persistence
- cross-school/IDOR tests
- PostgreSQL integration tests

## Never audit secrets
Passwords, password hashes, password tokens, API keys, SMTP/database credentials, cookies and Data Protection secrets must never be stored in audit records.

## Initial operation coverage
Schools, school users, roles, activation/locking, password invitation lifecycle metadata, academics, enrollments, curriculum, assessments/results, imports and existing manual Outbox administration.

## Acceptance
Build/tests/regression/PostgreSQL pass; success writes audit; failed or rolled-back mutations do not create false success audit; tenant isolation passes; sensitive values are redacted; correlation search works; EN/PL viewer works.
