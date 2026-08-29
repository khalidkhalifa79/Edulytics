# Phase 29 — Common Core Elementary Batch (Grades 1–5)

## Scope

Grades 1–5 are implemented together as one elementary pedagogical batch.

The authoritative Common Core curriculum pack remains unchanged:

- SchemaVersion: `14`
- NodeCount: `459`
- OfficialNodeCount: `393`
- numbered Standards: `385`
- Mathematical Practices: `8`
- Domain nodes: `66`

No curriculum-pack mutation, EF migration, or database wipe is required.

## Pedagogical source

- Source: IM K-5 Math, 1st Edition (2021)
- Publisher/source host: Illustrative Mathematics / Kendall Hunt
- License: CC BY 4.0
- Source-lock batch SHA-256: `3bf5fba93d0a0288658b1f58949dc3114eaede7bd775816a3e3aa4e47e4ffc9f`

Only the pedagogical sequence, lesson titles and source alignment metadata are captured.
Edulytics canonical explanations, examples, diagrams, common mistakes and summaries remain independently authored.

## Exact elementary graph

### Grade 1

- Units: `8`
- Lessons: `146`
- Formal mappings: `411`
- Official Grade Standards covered: `21`
- Zero-formal lessons: `2`
- Source-lock graph SHA-256: `1e3d264fc9d6b2da2d0b494c3379370d3216b967f497242c912e9a3fc3934eb8`
- Normalized blueprint graph SHA-256: `440cde99e64b19a409bcdc6a1c5cd33d38fc1bcef1f3b5273b1fc2540ae3898a`

### Grade 2

- Units: `9`
- Lessons: `146`
- Formal mappings: `330`
- Official Grade Standards covered: `26`
- Zero-formal lessons: `3`
- Source-lock graph SHA-256: `5566fcd85ca099f10cd62021c8d0cd778093b3789fe7f72d2a1b1a1cc6594c74`
- Normalized blueprint graph SHA-256: `21a401ad90fee5953763955922f81eac74e40cb80a480be5bdd201510edb7dbe`

### Grade 3

- Units: `8`
- Lessons: `143`
- Formal mappings: `222`
- Official Grade Standards covered: `25`
- Zero-formal lessons: `8`
- Source-lock graph SHA-256: `4ef968a9f5024e97199617425c7b18244886b12285498160e46284a5141648e0`
- Normalized blueprint graph SHA-256: `a4e226b0cca877a12679b3bae1a3347ef8eec6b0105445bac43c0d0b005eef5e`

### Grade 4

- Units: `9`
- Lessons: `149`
- Formal mappings: `253`
- Official Grade Standards covered: `28`
- Zero-formal lessons: `7`
- Source-lock graph SHA-256: `fe06ae8a0daabaa0ad36c8c6f956b2348705bd732873f52c60730731d61c03f1`
- Normalized blueprint graph SHA-256: `936b8d1f772ce196989cd6a39d60f0adac709169ec784da270f7a42d7f7aae43`

### Grade 5

- Units: `8`
- Lessons: `148`
- Formal mappings: `210`
- Official Grade Standards covered: `26`
- Zero-formal lessons: `5`
- Source-lock graph SHA-256: `7d99645b607df03449c2ceb9a64114f51634c1206632f0396ccf3dd1da19d848`
- Normalized blueprint graph SHA-256: `89c502822aeca4503e9a5f860d14c8cf2cf207df19a6df049d209484d3300899`

## Batch totals

- Units: `42`
- Lessons: `732`
- Formal mappings: `1426`
- Grade Standards: `126`

## Migration behavior

The five new embedded blueprints own the existing Common Core Grade 1–5 scopes.
The pedagogical seeder therefore stops generating one-outcome fallback lessons for those scopes.

Existing deterministic fallback lessons are removed only through the existing fail-closed stale-lesson path.
Deployment must stop if any obsolete Grade 1–5 fallback lesson is referenced by canonical lesson content.

The accepted Grade 6–8 source-driven graphs must remain unchanged.

## Safety

- no Common Core curriculum-pack mutation;
- no EF migration;
- no database wipe;
- no fuzzy alignment inference;
- no cluster/domain expansion into formal mastery targets;
- only explicit source `Addressing` alignments become formal OutcomeCodes;
- BuildingOn and BuildingTowards evidence remains provenance only unless explicitly resolved as Addressing;
- deployment requires an exact staging preflight before merge.
