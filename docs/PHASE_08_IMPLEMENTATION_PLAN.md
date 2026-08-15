# Edulytics — Phase 08 Implementation Plan

## Baseline
`609a660 feat: add curriculum framework foundation`

## Scope
Phase 08 implements:
- Assessment
- AssessmentQuestion
- QuestionLearningOutcome
- AssessmentResult
- StudentAnswer
- score entry and validation
- Assessment/AssessmentResult optimistic concurrency
- EN/PL responsive UI

CSV/Excel import remains Phase 11. Analytics remain Phase 09.

## Authorization
- School is the tenant boundary.
- SchoolAdmin can manage all assessments in the actor's own active school.
- Teacher can manage only an exact same-school ClassGroup + Subject pair for
  which TeacherAssignment exists.
- Student has no assessment administration access.
- SubjectSupervisor mutation is not added because the current academic model
  has no SubjectSupervisor-to-Subject assignment entity. Granting write access
  without that relation would not be safely scoped.

## Lifecycle
- Draft: assessment definition is editable; results cannot be entered.
- Open: questions/outcome mappings are locked; results can be entered.
- Closed: definition and results are read-only.

Opening requires:
- at least one question;
- sum(question MaxScore) equals Assessment.MaxScore;
- every question has at least one learning-outcome mapping.

## Score rules
- Scores use decimal, never float.
- Score columns use decimal(10,2); Percentage uses decimal(5,2).
- Assessment and question MaxScore must be > 0.
- StudentAnswer.Score must be 0..Question.MaxScore.
- AssessmentResult.Score is calculated from StudentAnswer rows.
- Percentage is calculated server-side and rounded to 2 decimals using
  MidpointRounding.AwayFromZero.
- Student must be enrolled in the assessment class for its academic year.
- One result per assessment/student.
- One answer per result/question.

## Outcome mapping
A LearningOutcome is eligible only when all of the following are true:
- same School as the Assessment;
- same Subject as the Assessment;
- same GradeLevel as the Assessment ClassGroup;
- the LearningOutcome FrameworkVersion is active;
- an active SchoolCurriculumAdoption exists for that exact School + Grade +
  Subject + FrameworkVersion.

Adoption resolution is deterministic:
1. if any active year-specific adoptions exist for the Assessment AcademicYear,
   only those year-specific adoptions are eligible;
2. otherwise active default adoptions (`AcademicYearId = null`) are used.

`IsPrimary` is not an exclusivity rule for question mapping; multiple actively
adopted framework versions may be mapped when the selected adoption scope
permits them.

## Concurrency
Assessment and AssessmentResult use SQL Server RowVersion.
Question/mapping mutations update the parent Assessment in the same unit of work
and use its RowVersion so stale definition edits do not silently overwrite.

## Migration
One Phase 08 migration:
`Phase08AssessmentsAndResults`

Expected new tables:
- Assessments
- AssessmentQuestions
- QuestionLearningOutcomes
- AssessmentResults
- StudentAnswers

The migration adds the tenant alternate key required for the composite
Assessment -> Term foreign key. The LearningOutcome tenant alternate key already
exists from Phase 07.5 and must be preserved. No DropTable/DropColumn is accepted
inside Up().

## UI
Routes are under `/school/assessments`.
All state-changing actions are POST + anti-forgery.
The School dashboard gets an Assessments and Results entry.
All UI strings are localized EN/PL.

## Verification
- Phase 08 xUnit tests
- full regression
- migration Up() safety inspection
- SQL Server database update
- controller persistence guard
- no-public-registration guard
- localization parity
- real SQL Server + Playwright acceptance
- assigned/unassigned teacher checks
- framework-aware question/outcome mapping
- year-specific adoption precedence + default adoption fallback
- score validation
- stale AssessmentResult RowVersion browser test
- responsive overflow checks at 320/375/480/768/1024/1280/1440
- manual visual gate
- commit, push, LOCAL == origin/main, clean working tree
