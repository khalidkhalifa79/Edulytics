# Phase 29 manual acceptance UI/visual hotfix

The previous Phase 29 staging deployment was technically successful, but human acceptance found defects in the lesson-library information hierarchy, lesson readability, raw-markup presentation, and instructional visual support. Phase 29 therefore remained **OPEN**.

This hotfix is presentation-only. It does not regenerate or translate the 1,560 accepted Common Core lesson bodies, change their official alignments, create outcome codes, or alter the supporting-lesson policy. Common Core academic content remains English while application controls and labels remain localized in English and Polish.

The hotfix:

- replaces redundant readiness and incomplete metrics with Total lessons, Officially aligned, and Supporting lessons;
- gives staff and students one shared, responsive educational reader presentation;
- safely decodes and reduces source markup to ordinary Razor-encoded text rather than rendering canonical HTML;
- extracts explicit figure descriptions at their pedagogical position;
- renders deterministic first-party instructional SVGs only for recognized source-described structures;
- removes display-only `Step N:` / `Krok N:` prefixes when the ordered list supplies numbering;
- preserves Supporting lessons as readable lessons without invented official outcomes;
- keeps source and licence attribution centralized rather than exposing legal metadata in individual lessons.

The deterministic audit is `tools/phase29-presentation-audit.py`. It scans all Common Core content packs and reports the 1,560 lessons seen, raw-markup inputs, explicit descriptions, mapped visual types, and the complete unsupported-description lesson list. Unsupported descriptions remain accessible plain text; they are not claimed as rendered diagrams. The two required Grade 6 regression lessons are covered by parser and visual contract tests.

Phase 30 is **NOT STARTED**. Final Phase 29 closure still requires a new deployment followed by successful human staging acceptance. This repository change does not commit, push, merge, or deploy anything.
