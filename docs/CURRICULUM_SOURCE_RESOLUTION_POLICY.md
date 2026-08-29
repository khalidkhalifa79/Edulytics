# Edulytics Curriculum and Lesson Source Resolution Policy

## Scope

This policy applies to every country, curriculum, subject, grade/stage,
pathway, academic year, term/version, pedagogical lesson, lesson-content pack,
assessment mapping, and future curriculum-dependent phase in Edulytics.

The target academic period for the current full-content rollout is 2026-2027.

## Two independent source responsibilities

Edulytics MUST distinguish between:

1. **Official academic authority source**
   - controls official curriculum identity;
   - controls official Standards / Learning Outcomes;
   - controls official codes where such codes exist;
   - remains the final authority for academic alignment.

2. **Pedagogical textbook/material source**
   - helps determine lesson sequencing;
   - helps determine lesson scope and age-appropriate depth;
   - provides a reference for commonly used examples and visual treatment;
   - may supply a real lesson title where a usable source title exists.

A commercial textbook never becomes an official government authority merely
because Edulytics uses it as a pedagogical source.

## Official academic source rule

For the official curriculum/standards layer:

1. Prefer the newest applicable official source.
2. Resolve at the smallest meaningful scope:
   country + curriculum + subject + grade/stage + pathway + term/version.
3. If the target version is available and verifiable:
   `CurrentOfficial`.
4. If the target official framework itself is unavailable, inaccessible,
   incomplete, or cannot be verified reliably, the most recent verifiable
   previous official version may be:
   `PreviousOfficialFallback`.
5. Previous official fallback metadata MUST state the target period, actual
   source period, authority, source version, URL, check timestamp, and reason.
6. A previous source must never be labelled as the current official source.
7. No official code, Standard, Learning Outcome, or official lesson identity
   may be invented.

## Pedagogical textbook priority — Source Policy v2

For each grade/stage/pathway and target academic year, choose the pedagogical
source in this order:

### Priority 1 — School-adopted textbook

If the school has selected a Mathematics textbook/material for the target
academic year (currently 2026-2027), use that book as the primary pedagogical
source.

Record evidence of school adoption.

### Priority 2 — Current official/ministry textbook or material

If no school-adopted textbook is known, use the applicable current
2026-2027 official Ministry/government/standards-authority textbook or
pedagogical material when such material exists.

### Priority 3 — Widely-used publisher textbook

If no suitable current school-adopted or official textbook/material exists,
select a reputable publisher textbook that:

- is aligned to the current official curriculum/standards;
- is applicable to the exact grade/stage/pathway;
- is current for the target period where possible;
- has evidence supporting that it is widely adopted/used.

"Most widely used" MUST NOT be asserted without recorded evidence.

### Priority 4 — Official framework only

Use the official Standards/Learning Outcomes directly only when no suitable
textbook/material source can be resolved after the above search.

In that case Edulytics constructs the pedagogical lesson sequence and lesson
titles from the verified official Outcomes/Standards.

## Lesson title rule

A published Edulytics lesson must have a meaningful pedagogical title.

Allowed title origins:

1. `PedagogicalSource`
   - title comes from the selected textbook/material source.

2. `EdulyticsDerivedFromOfficialOutcome`
   - used when the official source provides Standards/Outcomes but no usable
     lesson title;
   - title is authored by Edulytics from the exact aligned official outcome(s).

Generic shell names such as:

- `Lesson 01`
- `6.EE — Lesson 01`
- `Geometry — Lesson 05`

are not acceptable for Source Policy v2 Published content.

## Content authorship

Textbooks and official sources are references.

Unless explicit reuse rights have been verified:

- do not copy textbook prose wholesale;
- do not copy publisher diagrams wholesale;
- do not imply government or publisher authorship of Edulytics content.

Edulytics explanations, worked examples, solutions, common mistakes,
summaries, and original diagrams are Edulytics-authored/reviewed educational
content aligned to the selected sources.

## Publisher fallback evidence

When `WidelyUsedPublisherTextbook` is selected, the lesson-content pack MUST
record evidence supporting the selection.

Acceptable evidence can include, as applicable:

- school adoption lists;
- official approved-textbook lists;
- publisher adoption data;
- independent market/adoption data;
- documented broad school usage.

If evidence is insufficient, do not label a publisher "most widely used".

## Review status

`ReviewedBy = Edulytics Curriculum Review` means Edulytics reviewed its own
educational content against the recorded sources and checked:

- official curriculum/outcome alignment;
- lesson scope;
- mathematical correctness;
- examples and solutions;
- pedagogical level;
- title provenance;
- visual/diagram appropriateness when required;
- absence of unsupported official or publisher claims.

It does NOT mean that a government, Ministry, standards organization,
publisher, or school endorsed Edulytics.

## Product Owner decisions

The Product Owner has approved:

- target academic period 2026-2027 for the current rollout;
- school-adopted textbook first when known;
- current official textbook/material second;
- evidence-backed widely-used publisher textbook when required;
- official Outcomes/Standards as the final academic authority;
- Edulytics-authored pedagogical titles when a source provides outcomes but
  no usable lesson title;
- previous-official fallback only when the applicable current official
  framework itself cannot be obtained or verified reliably.

All fallbacks remain explicitly traceable and replaceable.

## Poland 2026-2027 rollout baseline decision

For the current Edulytics Poland Mathematics rollout, the Product Owner has
explicitly selected the complete 2025-2026 Polish curriculum baseline for all
supported Polish grades/stages/pathways.

This is a deliberate stability decision for the current rollout and not a claim
that the 2025-2026 source is the newest Polish curriculum.

Therefore:

- Edulytics operational target period remains `2026-2027`;
- Poland academic source period is `2025-2026`;
- Poland source resolution is recorded as `PreviousOfficialFallback`;
- the accepted curriculum pack remains `PL-MATH-2025-2026`;
- pedagogical textbook research for Poland must target books/materials aligned
  to the 2025-2026 curriculum baseline;
- no Polish 2026 transitional cohort is mixed into this Phase 29 rollout;
- no 2025-2026 source may be relabelled as a 2026-2027 official Polish source;
- a future Poland curriculum upgrade is handled as a separate reviewed source
  upgrade after the current full-content rollout.

Fallback reason:

`Product Owner selected the complete verified 2025-2026 Polish curriculum as
the stable baseline for the current rollout instead of mixing transitional
2026 curriculum cohorts.`
