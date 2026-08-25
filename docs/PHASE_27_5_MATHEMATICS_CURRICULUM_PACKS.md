# Edulytics — Phase 27.5 Mathematics Curriculum Packs

## Status model

Phase 27 remains closed for application production-readiness.

Phase 27.5 is a new pre-Oracle product-completeness phase.

Phase 28 remains post-real-go-live only.

## Locked curriculum scope

Mathematics only:
- England / British Mathematics
- American / Common Core Mathematics
- UAE Ministry of Education Mathematics
- Polish National Curriculum Mathematics

## Internal stages

### 27.5A — Foundation + official source registry
Framework codes, logical/native academic levels, provenance, reuse basis, attribution contracts, source verification and acceptance tests.

### 27.5B — England Mathematics
Years 1-11 use the DfE National Curriculum Mathematics programmes of study.
Years 12-13 are mapped to the separate DfE AS/A-level Mathematics subject-content source.
OGL v3.0 attribution is represented explicitly.

### 27.5C — American / Common Core Mathematics
K-12 is mapped to logical levels 1-13.
Official Mathematics HTML/PDF sources are registered and verified.
The product owner has explicitly confirmed commercial-use evidence.
The required copyright attribution is represented in code.

### 27.5D — UAE MoE Mathematics
Grades 1-12 only.
The Ministry is the authority.
The 2017 Mathematics Curriculum Standards Framework reference is preserved as a versioned historical curriculum source.
Current assessment material is used only for current scope/pathway metadata and is not substituted for curriculum outcomes.

### 27.5E — Polish Mathematics
Official ZPE 2025/2026 curriculum pages are registered.
Primary and upper-secondary Mathematics sources are distinct.
Liceum and Technikum pathways are preserved.

### 27.5F — Lessons + aggregate acceptance
The curriculum/standards layer remains the analytics source of truth.
A deterministic lesson-blueprint layer sits below standards/outcomes:
`StandardOrLearningOutcome -> Unit -> Lesson -> ActivityOrAssessmentQuestion`.

Every production lesson must link to one or more standards/outcomes in the same framework-version, Mathematics-subject and academic-level scope.

## Infrastructure boundary

Phase 27.5 does not:
- provision Oracle
- create paid Render production
- change Neon plans
- cut over DNS
- claim real production go-live
- start Phase 28

Oracle work resumes only after all pre-go-live product work is accepted and the product owner explicitly approves production provisioning.
