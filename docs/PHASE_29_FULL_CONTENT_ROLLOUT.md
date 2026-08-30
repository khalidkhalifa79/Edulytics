# Phase 29 — Full Mathematics Lesson Content Rollout

## Current authoritative decision (2026-08-30)

Phase 29 is a LOCAL CLOSURE CANDIDATE. Protected PR CI, merge, merged-main CI,
Render staging, and human browser/content acceptance remain pending. Phase 30
is NOT STARTED.

Curriculum content stays in its official/source academic language; Edulytics
UI localization is independent. Common Core canonical content is English.
The former Polish Common Core translation requirement is cancelled and not
applicable. See `architecture/CURRICULUM_LANGUAGE_POLICY.md`.

## Curriculum tracks

1. UAE Ministry of Education Mathematics
2. England Mathematics / National Curriculum
3. US Common Core State Standards for Mathematics
4. Poland national Mathematics curriculum

Coverage follows the real grade/stage structure of each education system.
Edulytics does not invent a Grade 13 where that system does not define one.

## Current target period

2026-2027.

## Source hierarchy

Every content batch follows `CURRICULUM_SOURCE_RESOLUTION_POLICY.md`.

### Poland baseline exception

For the current Phase 29 full-content rollout:

- Edulytics operational target period remains 2026-2027;
- every Poland Mathematics grade/stage/pathway uses the complete verified
  2025-2026 Polish curriculum baseline;
- accepted Poland curriculum pack is `PL-MATH-2025-2026`;
- Poland is recorded as `PreviousOfficialFallback`;
- Poland textbook acquisition targets 2025-2026-aligned books/materials;
- the 2026 transitional curriculum is not mixed into this rollout.

The active grade/stage/pathway source status is maintained in:

`docs/PHASE_29_SOURCE_ACQUISITION_MATRIX.json`


Pedagogical source priority:

1. school-adopted target-year textbook;
2. current target-year official/ministry textbook/material;
3. evidence-backed widely-used current publisher textbook;
4. official framework/outcomes only when no suitable textbook can be resolved.

Official Standards / Learning Outcomes remain the final academic authority.

## Required lesson quality

Every Published lesson must have:

- meaningful lesson title;
- traceable title provenance;
- exact official Outcome/Standard alignment;
- Explanation;
- Key Concepts / Rules;
- Worked Examples;
- Step-by-Step Solutions;
- Common Mistakes;
- Quick Summary;
- mathematically appropriate notation;
- diagrams/visual aids when the concept materially benefits from them;
- source provenance;
- Edulytics curriculum review evidence.

Generic `Lesson 01` shells are not Production Ready.


### Supporting pedagogical lessons

A source-valid pedagogical lesson that has no formal official OutcomeCode is
not automatically an error. It may be retained only when that zero-formal
state is locked by the accepted pedagogical source blueprint.

Edulytics MUST NOT fabricate an official Standard / Learning Outcome mapping
to make such a lesson independently publishable.

Supporting-only lessons are:

- preserved in the pedagogical source sequence;
- included in the overall Content Ready denominator;
- exposed as independent Supporting lessons to authorized staff and enrolled
  students;
- rendered with their full canonical body and no fabricated official outcome.

Common Core currently locks 94 such source-valid supporting lessons.

Kindergarten remains present only in the verified official Common Core
framework and is outside the Edulytics product lesson scope.

The Common Core product rollout therefore begins at Grade 1 and contains:

- 1,560 in-scope pedagogical lessons;
- 1,466 standalone canonical-content targets;
- 94 source-valid supporting-only lessons;
- 0 Kindergarten product pedagogical lessons.

See `PHASE_29_COMMON_CORE_SUPPORTING_LESSON_POLICY.md`.

## Common Core repository content checkpoint

The US Common Core repository candidate is complete only when the automated
Phase 29 audit proves all of the following simultaneously:

- 1,560 in-scope pedagogical lessons;
- 1,466 officially aligned lessons represented by Published English content;
- 94 Published English Supporting lessons with zero independent OutcomeCodes;
- zero Kindergarten product lessons;
- exact blueprint-to-Standard mappings with no fabricated OutcomeCode;
- approved OER provenance and traceable review evidence for every content pack;
- deterministic mathematical verification for generated worked examples;
- final full regression, PR CI, main CI, automatic staging seed, and staging DB audit.

Overall Common Core Content Ready coverage is **1,560 / 1,560**. The aligned
count (1,466) is not the readiness denominator. Phase 30 remains not started.

## Rollout workstreams

### 29A — Engine and rendering

Complete lesson rendering, mathematical formatting, visual/diagram support,
source metadata, and review contracts.

### 29B — Source acquisition and mapping

Build the evidence-backed source map for every:
country + curriculum + grade/stage + pathway + target period.

### 29C — UAE full rollout

Populate and review supported UAE Mathematics lessons.

### 29D — England full rollout

Populate and review supported England Mathematics lessons.

### 29E — Common Core full rollout

Populate and review supported Common Core Mathematics lessons.

### 29F — Poland full rollout

Populate and review supported Poland Mathematics lessons.

### 29G — Completeness and QA audit

No Phase 29 closure until the supported rollout audit reports:

- no unreviewed generic lesson titles;
- no fabricated official codes/outcomes;
- no missing required content body sections;
- source selection traceable;
- publisher fallback supported by evidence;
- official outcome alignment valid;
- required visual lessons have an appropriate visual treatment;
- all supported rollout lessons are Production Ready.

## Relationship to later phases

Phase 30 begins only after Phase 29 closure.

The resulting full lesson-content corpus becomes the curriculum-grounded input
for the later item, assessment, mastery, diagnostic, adaptive, and weakness-
recovery engines.
