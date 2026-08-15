# Edulytics — Phase 07.5 Implementation Plan

## Scope

Architecture-only curriculum framework foundation between accepted Phase 07
and Phase 08.

Implement:
- CurriculumFramework
- CurriculumFrameworkVersion
- SchoolCurriculumAdoption
- FrameworkVersion-aware CurriculumTopic
- framework/grade/subject-aware LearningOutcome
- safe migration and compatibility backfill
- automatic default adoption for the existing Topic creation workflow
- tests for multi-framework and multi-grade identity rules

No visible curriculum-management feature is added. Existing Phase 07 UI and
SchoolAdmin authorization remain unchanged.

## Acceptance

- accepted Phase 07 baseline
- 118-test baseline green before changes
- build green
- Phase 07 regression green after amendments
- Phase 07.5 tests green
- full regression green
- no DropTable/DropColumn in migration Up()
- SQL Server migration applies
- existing Topic/Outcome rows have valid non-empty framework scope
- same Topic name/order allowed across FrameworkVersions
- same Outcome code allowed across FrameworkVersions
- same Outcome code allowed across Grades
- duplicate Outcome code rejected in exact framework+grade+subject scope
- tenant-safe Grade/Subject references on adoption
- controller remains DbContext-free
- localization parity remains green
- commit/push only after final verification
