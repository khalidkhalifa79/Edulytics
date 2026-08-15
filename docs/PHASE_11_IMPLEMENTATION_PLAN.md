# Edulytics — Phase 11 Data Import

Baseline:

`56d26bf feat: add real-time analytics updates`

## Required workflow

Upload
→ parse
→ validate
→ preview
→ confirm
→ apply transactionally
→ audit/history
→ analytics refresh
→ dashboard notification

## Supported import types

1. Students
2. Teachers
3. Classes
4. Subjects
5. Assessment results
6. Curriculum mappings

## Formats

- CSV UTF-8
- XLSX

Limits:

- 5 MiB file
- 10,000 data rows
- 100 columns
- preview first 100 rows

## Schemas

Students:

`StudentNumber,FirstName,LastName,AcademicYear,ClassCode`

Teachers:

`Email,AcademicYear,ClassCode,SubjectCode`

Teachers import creates TeacherAssignment records only for already provisioned,
active Teacher accounts. It does not bypass Phase 05 account/invitation security.

Classes:

`AcademicYear,GradeLevel,Code,Name`

Subjects:

`Code,Name`

Assessment results:

`AssessmentId,StudentNumber,QuestionOrder,Score`

One row represents one question score. Every assessment question must occur
exactly once for a student result.

Curriculum mappings:

`AssessmentId,QuestionOrder,OutcomeCode`

## Security

- School is the tenant boundary.
- SchoolAdmin can use all import types.
- Teacher can use AssessmentResults only.
- Teacher result rows require exact ClassGroup + Subject assignment.
- SubjectSupervisor and Student are not granted Phase 11 access.
- All state-changing actions use POST + anti-forgery.
- Controllers contain no DbContext.
- No public registration.

## Import safety

ImportBatch persists:

- SchoolId
- ImportType
- status
- filename
- SHA-256 hash
- parsed normalized rows
- row/error counts
- uploader/completer
- timestamps
- RowVersion

ImportValidationError persists:

- SchoolId
- ImportBatchId
- row
- column
- validation code
- sanitized raw value

Same user + school + import type + identical file hash is idempotent.

Confirm:

- revalidates current database state;
- uses ImportBatch RowVersion;
- verifies RowVersion/status for referenced editable entities;
- runs one SQL transaction;
- commits all rows or none;
- records ImportBatchCompleted in the Phase 10 outbox.

The Phase 10 background processor handles ImportBatchCompleted by rebuilding
analytics and notifying the server-derived school/admin and affected
class/subject teacher SignalR groups.

## Existing-data behavior

Imports are insert-oriented.

Conflicting existing records are validation errors rather than implicit
overwrites.

Assessment results already present for a student/assessment are conflicts.

Curriculum mappings require Draft assessments.

## Acceptance

- CSV parser tests
- XLSX parser tests
- exact schema tests for all six import types
- plan-generation tests for all six import types
- validation tests
- tenant isolation tests
- Teacher result-only permission tests
- EF mapping tests
- localization parity tests
- additive migration
- SQL Server migration
- real browser validation preview
- real XLSX confirmation
- duplicate-file idempotency
- real assessment-result import
- transactional result/outbox evidence
- analytics recalculation
- SignalR dashboard refresh
- cross-school batch isolation
- EN/PL responsive browser verification
- manual visual gate
- final build/regression/security/vulnerability audit
- commit + push only after all acceptance gates pass

Phase 12 must not begin.
