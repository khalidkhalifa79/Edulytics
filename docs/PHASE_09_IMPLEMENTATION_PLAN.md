# Edulytics — Phase 09 Analytics Implementation Plan

## Baseline

`60d9d44 feat: localize product branding`

## Scope

Phase 09 implements a production-oriented analytics/read-model layer over the
Phase 08 assessment/result source data.

Included:

- student learning-outcome mastery;
- class learning-outcome summaries / heatmap;
- class topic weakness analysis;
- progress over time;
- student risk indicators;
- school dashboard snapshot;
- explicit idempotent analytics recalculation;
- SchoolAdmin and assignment-scoped Teacher analytics access;
- EN/PL localized responsive analytics dashboard.

Excluded:

- SignalR / live push updates — Phase 10;
- background/outbox processing — Phase 10 when required;
- CSV/Excel import — Phase 11;
- production monitoring/hardening — Phase 12.

## Raw data vs projections

Raw academic data remains authoritative and is never mutated by analytics:

- Assessment;
- AssessmentQuestion;
- QuestionLearningOutcome;
- AssessmentResult;
- StudentAnswer.

Phase 09 persists calculated projections separately:

- StudentOutcomeMastery;
- ClassOutcomeSummary;
- ClassTopicSummary;
- ClassAssessmentTrend;
- SchoolAnalyticsSnapshot.

Dashboard reads use these projection tables, not raw assessment-table
recalculation on every request.

## Mastery calculation

Evidence comes from StudentAnswer.

For every answer:

1. Resolve its AssessmentResult.
2. Resolve its non-Draft Assessment.
3. Resolve its AssessmentQuestion.
4. Resolve QuestionLearningOutcome mappings.
5. Split both earned and possible score equally across every mapped outcome.

Equal allocation prevents a question mapped to multiple learning outcomes from
double-counting raw score while preserving the question percentage.

Student outcome mastery:

`sum(allocated earned) / sum(allocated possible) * 100`

Percentage is rounded to two decimals using:

`MidpointRounding.AwayFromZero`.

## Mastery bands

- 0–39.99: Critical gap
- 40–59.99: Weak
- 60–74.99: Developing
- 75–89.99: Secure
- 90–100: Strong

## Assessment lifecycle inclusion

- Draft assessments are excluded.
- Open assessments contribute submitted evidence.
- Closed assessments contribute submitted evidence.

This lets explicit Phase 09 recalculation reflect current entered evidence.
Automatic live recalculation remains Phase 10.

## Topic calculation

Per-outcome mastery remains an evidence percentage and is not distorted by the
curriculum weight.

When rolling class outcomes into topic mastery, LearningOutcome.Weight is used
as the normalized aggregation weight.

## Risk

A student is At Risk when mastery for the current filtered projection scope is
below 60%.

Critical learning-outcome gaps are below 40%.

## Projection replacement / idempotency

Recalculation:

- reads one school only;
- produces a complete projection set;
- replaces that school's existing projections transactionally;
- is safe to execute repeatedly;
- never modifies raw assessment data.

SQL Server replacement uses a serializable transaction.

## Authorization

School remains the tenant boundary.

SchoolAdmin:
- can read all analytics for own active school;
- can trigger recalculation.

Teacher:
- can read only analytics for exact ClassGroup + Subject pairs represented by
  TeacherAssignment;
- cannot recalculate school projections.

SubjectSupervisor:
- is not granted broad analytics access in Phase 09 because the current domain
  has no SubjectSupervisor-to-Subject assignment entity.
- granting whole-school analytics would violate least privilege and the tenant /
  role boundary.

Student:
- no Phase 09 analytics dashboard.

## Staleness

Dashboard compares the latest AssessmentResult / StudentAnswer source update
against the latest projection generation time.

SchoolAdmin receives a localized stale-data indicator and can explicitly
recalculate.

## UI

Route:

`/school/analytics`

Dashboard includes:

- filters: academic year, class, subject;
- overall mastery;
- students with evidence;
- at-risk count;
- critical outcome count;
- weak topic count;
- class mastery heatmap;
- topic analysis;
- progress over time;
- student risk list;
- mastery legend;
- projection generated timestamp / stale state.

## Migration

One additive migration:

`Phase09Analytics`

No existing Phase 08 raw table is dropped or altered destructively.

## Verification

- build;
- Phase 09 tests;
- full regression;
- projection formula tests;
- multi-outcome no-double-count test;
- mastery boundary tests;
- tenant and TeacherAssignment scope tests;
- recalculation authorization;
- idempotent repository replacement;
- EF model/index checks;
- EN/PL resource parity;
- controller DbContext guard;
- no-public-registration guard;
- migration Up() destructive-operation guard;
- real SQL Server database update;
- Playwright EN/PL acceptance;
- Teacher assignment isolation;
- unassigned Teacher no-data isolation;
- responsive overflow checks at:
  320 / 375 / 480 / 768 / 1024 / 1280 / 1440;
- manual visual acceptance before commit;
- final full verification;
- commit / push / LOCAL == origin/main / clean tree.
