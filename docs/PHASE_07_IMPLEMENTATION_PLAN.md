# Edulytics — Phase 07 Implementation Plan

## Baseline

`6dd6b85 feat: add academic structure management`

## Scope

Phase 07 implements Curriculum and Learning Outcomes:

- Grade 6 Mathematics curriculum management structure.
- Curriculum topics.
- Learning outcomes.
- Stable outcome codes.
- Outcome weighting.
- Readiness for Phase 08 outcome-to-question mapping.

The authoritative specification does not contain the actual Polish Grade 6
Mathematics syllabus text. Phase 07 therefore does not fabricate production
curriculum content. It implements the full management model and validates it
with temporary acceptance data.

## Data model

CurriculumTopic:
- Id
- SchoolId
- SubjectId
- GradeLevelId
- Name
- Order

LearningOutcome:
- Id
- SchoolId
- TopicId
- Code
- Description
- Weight
- Order

Decisions:
- Phase 07 persists school-scoped curriculum only.
- Platform templates are deferred because Phase 06 Subject and GradeLevel are
  school-scoped and the current specification does not define a platform
  Subject/Grade template model.
- Learning outcome codes are normalized to uppercase.
- Weight is stored as a decimal percentage greater than 0 and <= 100.
- No sum-to-100 rule is invented because the specification does not define one.
- Assessment/question entities remain Phase 08.

## Tenant and authorization

- School is the tenant boundary.
- SchoolAdmin manages curriculum for their own active school.
- SubjectSupervisor, Teacher and Student cannot mutate curriculum.
- Cross-school Subject, GradeLevel, Topic and Outcome references are rejected.
- Suspended/archived schools cannot mutate curriculum.
- Controllers are thin and contain no DbContext or SQL.
- Every state-changing request is POST + anti-forgery.

## Architecture

Core:
- CurriculumTopic
- LearningOutcome
- curriculum persistence contracts
- ICurriculumRepository

Data:
- EF configurations
- CurriculumRepository
- DbContext registration
- one Phase 07 migration

Services:
- ICurriculumService
- CurriculumService
- validation and tenant rules

Web:
- CurriculumController
- CurriculumResource EN/PL
- view models
- responsive index/edit views
- School dashboard entry point

Tests:
- EF model/composite tenant foreign keys
- service validation and isolation
- role restrictions
- duplicate topic/outcome rules
- weight validation
- localization parity
- anti-forgery/authorization contracts
- responsive CSS contract
- real Playwright acceptance

## Migration

`Phase07CurriculumLearningOutcomes`

Expected tables:
- CurriculumTopics
- LearningOutcomes

No DropTable/DropColumn is accepted.

## Acceptance

Automated:
- build
- Phase 07 tests
- full regression
- migration inspection
- SQL Server database update
- architecture/security guards
- EN/PL parity
- Playwright functional acceptance
- horizontal-overflow checks at all required widths

Manual visual gate before commit:
- EN + PL screenshots at 320/375/480/768/1024/1280/1440+
- no clipping
- no overlap
- no hidden primary actions
- no mixed localization

Only after manual ACCEPT:
- final verification
- commit
- push
- local/remote equality check
