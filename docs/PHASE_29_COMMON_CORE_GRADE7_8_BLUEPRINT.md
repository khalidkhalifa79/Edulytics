# Phase 29F/G — Common Core Grades 7–8 Source-Driven Batch

## Scope

This batch extends the already-verified Common Core Grade 6 source-driven
architecture to Grades 7 and 8 using the same Open Up Resources / IM 6–8
Mathematics source family.

## Grade 7

- Units: 9
- Lessons: 145
- Aggregate standards-table rows: 144
- Official numbered Standards covered by Addressing: 24/24
- Formal Addressing mappings: 199
- Zero-formal lessons: 7
- No numbered Grade-7 Standard in any preparation role: 1
- Multi-standard lessons: 61
- Semantic graph SHA-256: `243f2fc1a433bd8488bb8577579d92d045ac98cb136ae6066b70c659fc85be37`

Grade 7 lesson `7.6.23` is present in the source lesson inventory but absent
from the aggregate Lessons and Standards table. Its preparation page has no
formal numbered Addressing target, so the pedagogical lesson is retained with
zero formal mastery mappings.

## Grade 8

- Units: 9
- Lessons: 131
- Aggregate standards-table rows: 131
- Official numbered Standards covered by Addressing: 28/28
- Formal Addressing mappings: 146
- Zero-formal lessons: 22
- No numbered Grade-8 Standard in any preparation role: 16
- Multi-standard lessons: 46
- Semantic graph SHA-256: `cfab43f2bb82317c3366e80ddec79128b1777c2bbbe8d947ec627654b5e21d85`

## Source semantics

Preparation-page roles are preserved exactly as:

- BuildingOn
- Addressing
- BuildingTowards

Only current-grade numbered `Addressing` references become formal Edulytics
mastery targets.

Explicit numbered subparts may resolve to their accepted numbered parent.

Clusters and domains are never expanded into invented numbered Standards.

The published Lessons and Standards table is retained as an independent
aggregate lesson/activity cross-check. It is not substituted for the role graph
on each preparation page.

## Safety

- Grade 6 source blueprint is not rewritten.
- Official Common Core curriculum pack is unchanged.
- MathematicsPedagogicalLessonSeeder is unchanged.
- No EF migration.
- No database wipe.
- Existing stale pseudo-lessons remain protected by the canonical-content
  reference guard before deletion.
- No textbook lesson-body or assessment content is copied.

## Result

After successful runtime verification, Common Core Grades 6, 7 and 8 are one
complete source-driven middle-school family.
