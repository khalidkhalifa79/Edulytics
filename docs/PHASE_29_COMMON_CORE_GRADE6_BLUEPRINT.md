# Phase 29E — Common Core Grade 6 Source-Driven Blueprint

## Scope

- Official pack: `US-CCSS-MATH`
- Framework version: `CCSSM-2010`
- Native level: Grade 6
- Logical level: 7
- Blueprint: `US-CCSS-MATH:G6:OUR-IM-2017`

The accepted Common Core pack remains the official academic authority.

## Source lock

The pedagogical sequence is based on the Open Up Resources / IM 6–8 Math
2017–2019 CC BY 4.0 lineage.

The durable source lock uses curriculum-semantic fingerprints rather than raw
HTML byte hashes.

Locked semantic graph SHA-256:

`edae65cc700ae2b2f3a5a7828275a3ffdded4fbf07759489801e7c4e5059e0e9`

Verified source graph:

- 9 units
- 147 lessons
- 29 accepted Grade 6 numbered Standards
- 29/29 Addressing coverage
- 208 formal Addressing lesson/outcome rows
- 17 lessons without a numbered Addressing target
- 10 lessons without Addressing or BuildingTowards numbered targets
- 9 lessons without a numbered Grade 6 Standard in any source role
- 60 multi-Standard lessons

## Alignment semantics

The source roles remain distinct:

- BuildingOn
- Addressing
- BuildingTowards

Only exact Grade 6 numbered Addressing references become formal Edulytics
mastery targets.

An explicit source subpart may resolve to its accepted numbered parent
Standard, with `SubpartToAcceptedParent` recorded in the blueprint.

Clusters and domains are never expanded into invented numbered Standards.

BuildingOn and BuildingTowards references remain source-alignment metadata and
are not promoted into mastery targets.

Therefore a source-driven pedagogical lesson supports 0..N formal mappings.

## Runtime transition

The old Common Core Grade 6 one-Standard-per-lesson pseudo sequence becomes
obsolete.

Startup creates the 147 deterministic source-driven lessons and their exact
formal mappings.

Obsolete pseudo-lessons are removed only when no canonical lesson content
references them. Existing referenced content causes a fail-closed startup
instead of silent data loss.

No EF schema migration is required. The existing relationship table already
supports zero or many lesson/outcome rows.

## Content boundary

This phase does not copy or publish textbook lesson bodies or assessments.

Edulytics lesson explanations, key concepts, worked examples, step-by-step
solutions, common mistakes, summaries and diagrams remain independently
authored and reviewed.

The 17 lessons with no exact numbered Addressing target are retained as real
pedagogical lessons. Edulytics does not invent a Standard merely to satisfy a
database cardinality.
