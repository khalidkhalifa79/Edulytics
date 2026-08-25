# Phase 27.5 — Verified Curriculum Persistence after Source v11

This change starts from the accepted source-only v11 contract. If the transient v11 files are missing, the runner rehydrates them and validates the same stable source contract.

Digest note:
- England, Common Core and Poland retain their accepted v11 content digests.
- The original v11 UAE runtime digest included volatile live mirror-response metadata (resolved URL, response bytes/hash, media type and status), so it is preserved only as historical run evidence and is not used as an immutable curriculum identity.
- UAE persistence uses a deterministic semantic source digest over the fixed Grade 1-12 / 2026-2027 Term 1 locator, track and explicit-code policy contract: `4325a43a2ac13b36502a2f694a896dc889fa8777c84364d12819ee4fee54cfe6`.

Persisted verified content:
- England Mathematics: 436 official items.
- Common Core Mathematics: 360 official items (352 content + 8 practices).
- Polish Mathematics: 306 official top-level requirements.
- UAE Mathematics user-facing version: 2026–2027, Term 1 source catalog across Grades 1–12.
- UAE Grade 9 Advanced Term 1: 42 real lessons, 6 units and 48 lesson-to-standard links using 22 evidenced UAE MoE standard codes.

UAE rule:
- never invent a standard code;
- prefer current authoritative code evidence;
- where the current 2026–2027 lesson matches a historical UAE MoE learning outcome exactly, an existing official historical UAE code may be used;
- historical source year remains internal provenance and is not shown as the curriculum version.

Important completeness boundary:
The Grade 1–12 current UAE source catalog is persisted, but real lesson-level rows are persisted only where they have actually been verified. This change does **not** synthesize fake lessons for UAE grades/tracks that have not yet been crosswalked.

The persistence model is platform-global and separate from school-scoped `CurriculumTopic` / `LearningOutcome`. A many-to-many link table permits one real lesson to align with multiple standards.

No Oracle provisioning, DNS cutover, paid Render/Neon change, production cutover, or Phase 28 work is part of this change.
