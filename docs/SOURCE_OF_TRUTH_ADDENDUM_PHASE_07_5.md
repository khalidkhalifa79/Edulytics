# Edulytics — Source-of-Truth Addendum: Curriculum Framework Foundation

Status: APPROVED FOR IMPLEMENTATION

Baseline: `981e834 feat: add curriculum and learning outcomes`

## Purpose

Phase 07 introduced `CurriculumTopic` and `LearningOutcome` correctly for the
initial Poland / Grade 6 / Mathematics scope. Its identity rules, however,
assume one curriculum family per School + Grade + Subject.

Before Phase 08 links assessments and results to learning outcomes, Edulytics
must be able to distinguish curriculum frameworks and versions without
changing the initial MVP content.

This addendum does not add Cambridge, CCSS, American, IB or any other
curriculum content. It only adds the architectural dimension required to
support those frameworks later.

## CurriculumFramework

Fields:
- Id
- OwnerSchoolId nullable
- Code
- NormalizedCode
- Name
- CountryCode nullable
- ProviderName nullable
- IsActive
- CreatedAtUtc
- UpdatedAtUtc
- RowVersion

`OwnerSchoolId = null` means a platform/global framework.
`OwnerSchoolId != null` reserves the model for future school-owned custom
frameworks. Phase 07.5 does not add framework-management UI.

## CurriculumFrameworkVersion

Fields:
- Id
- FrameworkId
- VersionCode
- NormalizedVersionCode
- Name
- EffectiveFrom nullable
- EffectiveTo nullable
- IsActive
- CreatedAtUtc
- UpdatedAtUtc
- RowVersion

A curriculum revision that must preserve historical meaning creates a new
version rather than silently changing historical outcome identity.

## SchoolCurriculumAdoption

Fields:
- Id
- SchoolId
- AcademicYearId nullable
- GradeLevelId
- SubjectId
- FrameworkVersionId
- IsPrimary
- IsActive
- CreatedAtUtc
- UpdatedAtUtc
- RowVersion

Rules:
- AcademicYearId null = default adoption across academic years.
- Multiple framework versions may be adopted for one Grade + Subject.
- One primary adoption is allowed for the exact School + AcademicYear scope +
  Grade + Subject.

## CurriculumTopic amendment

Add `FrameworkVersionId`.

Unique scope becomes:
- School + FrameworkVersion + Subject + Grade + Name
- School + FrameworkVersion + Subject + Grade + Order

## LearningOutcome amendment

Add:
- FrameworkVersionId
- SubjectId
- GradeLevelId

The duplicated scope is intentional so the database can enforce the full
Outcome -> Topic relationship.

Outcome code uniqueness becomes:
- School + FrameworkVersion + Subject + Grade + Code

Therefore the same code may exist in another framework, grade or subject, but
not twice inside one exact curriculum scope.

## Current UI compatibility

Phase 07.5 is foundation-only. Existing curriculum screens remain unchanged.
When the existing Create Topic workflow is used:
1. the service resolves the primary default adoption for Grade + Subject;
2. if none exists, it creates a default adoption to the platform compatibility
   framework/version;
3. the new Topic is assigned to that FrameworkVersion.

This preserves the current MVP workflow while removing the architectural
single-framework restriction.

## Compatibility framework

Migration creates:
- Framework code: EDULYTICS-DEFAULT
- Version code: V1

Existing Phase 07 topics/outcomes are backfilled into this version. Existing
curriculum data is not deleted.

## Phase 08 requirement

Phase 08 must be regenerated from the post-07.5 baseline.
Question-to-outcome eligibility must include School + Subject + Grade and an
active SchoolCurriculumAdoption for the LearningOutcome FrameworkVersion.
