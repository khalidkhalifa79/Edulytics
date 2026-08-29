# Phase 29 — Common Core Official Pack Integrity Repair

## Authoritative finding

The accepted `US-CCSS-MATH / CCSSM-2010` snapshot contained a systemic PDF parsing-boundary defect.

The authoritative ADA Common Core Mathematics source was verified with SHA-256:

`1dc360aa21390c2860c939f731b693295ee1537cbb2b2e3be2ccd06dcb06898c`

Full numbered-Standard audit:

- K–8 numbered Standards: `229`
- High School numbered Standards: `156`
- total numbered Standards: `385`
- Mathematical Practices: `8`
- corrected official count: `393`

Comparison of all 352 numbered Standards that existed in the legacy pack:

- exact source text: `212`
- exact authoritative prefix plus trailing contamination: `140`
- truncated: `0`
- divergent: `0`

Therefore the 140 affected texts were safe to repair by replacing the contaminated value with its already-proven authoritative prefix.

## Historical accepted fingerprint

- NodeCount: `420`
- OfficialNodeCount: `360`
- SourceDigest: `5f80bbc801950969d2f4085cd31b113851fd09fc07d3066d5395eb7102c9df36`
- ContentDigest: `e265387773923fd77eed959d49fa87e983171d917b894c2cff7c8ca2085526d5`
- Domain nodes: `60`

## Previously corrected accepted fingerprint

Before the Grade 1 follow-up, the accepted corrected state was:

- NodeCount: `458`
- OfficialNodeCount: `392`
- Domain nodes: `66`
- numbered Standards: `384`
- K–8 numbered Standards: `228`
- High School numbered Standards: `156`
- ContentDigest: `5cab24d5f9b3d27839207db19ca8182d69acca4c4e252e3d8da822cb64541e82`

That fingerprint remains an explicitly accepted historical startup-upgrade source.

## Final corrected accepted fingerprint

- SchemaVersion: `14`
- NodeCount: `459`
- OfficialNodeCount: `393`
- Domain nodes: `66`
- numbered Standards: `385`
- K–8 numbered Standards: `229`
- High School numbered Standards: `156`
- ContentDigest: `ad2fbce7de0dce8a3e768de301f6395d82900789da14dce901e5fe68e0a947a9`

The authoritative source identity did not change, so `SourceDigest` remains unchanged.

## Restored nodes

The original authoritative repair added exactly 38 nodes: 32 Standards and 6 Domains.

The Grade 1 follow-up adds one additional official Standard,
`CCSS:1.OA.B.3`, making the complete repair lineage 39 restored nodes:
33 Standards and 6 Domains.

- `CCSS:1.OA.B.3`

- `CCSS:6.RP.A.1`
- `CCSS:6.RP.A.2`
- `CCSS:6.RP.A.3`
- `CCSS:6.SP.A.1`
- `CCSS:6.SP.A.2`
- `CCSS:6.SP.A.3`
- `CCSS:6.SP.B.4`
- `CCSS:6.SP.B.5`
- `CCSS:7.RP.A.1`
- `CCSS:7.RP.A.2`
- `CCSS:7.RP.A.3`
- `CCSS:7.SP.A.1`
- `CCSS:7.SP.A.2`
- `CCSS:7.SP.B.3`
- `CCSS:7.SP.B.4`
- `CCSS:7.SP.C.5`
- `CCSS:7.SP.C.6`
- `CCSS:7.SP.C.7`
- `CCSS:7.SP.C.8`
- `CCSS:8.SP.A.1`
- `CCSS:8.SP.A.2`
- `CCSS:8.SP.A.3`
- `CCSS:8.SP.A.4`
- `CCSS:DOMAIN:10-13:HSS-CP`
- `CCSS:DOMAIN:7-7:6.RP`
- `CCSS:DOMAIN:7-7:6.SP`
- `CCSS:DOMAIN:8-8:7.RP`
- `CCSS:DOMAIN:8-8:7.SP`
- `CCSS:DOMAIN:9-9:8.SP`
- `CCSS:HSS-CP.A.1`
- `CCSS:HSS-CP.A.2`
- `CCSS:HSS-CP.A.3`
- `CCSS:HSS-CP.A.4`
- `CCSS:HSS-CP.A.5`
- `CCSS:HSS-CP.B.6`
- `CCSS:HSS-CP.B.7`
- `CCSS:HSS-CP.B.8`
- `CCSS:HSS-CP.B.9`

## Integrity manifest

`us-ccss-math.integrity-manifest.json` is embedded with the application.

It records:

- the verified official PDF SHA-256;
- the corrected pack ContentDigest;
- exact corrected cardinalities;
- the exact original 420/360 historical 38-node missing set;
- the exact previously-corrected 458/392 fingerprint, where only `CCSS:1.OA.B.3` is missing;
- the exact historical ContentHash values required to validate the V14 SortOrder transition;
- legacy ContentHash values for all 140 corrected existing Standards;
- SHA-256 of the accepted official text for every one of the 385 numbered Standards.

This allows automated regression checks without downloading the source PDF during normal CI/startup.

## Database repair safety

No database wipe is permitted.

No EF migration is required.

The startup seeder may upgrade an existing Common Core installation only when its import state exactly matches one of two explicitly accepted historical fingerprints:

- original historical `420 / 360`; or
- previously corrected `458 / 392`.

For the `420 / 360` path it requires the original 38-node missing set plus the follow-up `CCSS:1.OA.B.3` absence. For the `458 / 392` path it requires exactly `CCSS:1.OA.B.3` to be missing.

Both paths additionally require:

- no stale/unexpected node codes;
- deterministic ids for every existing node;
- unchanged static metadata apart from the explicitly accepted V14 SortOrder transition;
- exact historical ContentHash fingerprints for every row that requires content or ordering repair.

Unrelated drift remains rejected.

The operation remains fail-closed for any unrecognized drift.

On PostgreSQL the existing Phase 27.5 advisory-lock transaction wraps the repair.

## Pedagogical lesson preservation

All existing official-pack nodes retain their deterministic ids.

`CCSS:1.OA.B.3` is inserted at semantic `SortOrder 20`, immediately before `CCSS:1.OA.B.4`. Existing pack nodes from the previous SortOrder 20 onward move forward by exactly one position and receive canonically recomputed ContentHash values.

The pedagogical lesson generated for `CCSS:1.OA.B.3` is new. The five existing Grade 1 OA fallback lessons after that insertion (`1.OA.B.4`, `1.OA.C.5`, `1.OA.C.6`, `1.OA.D.7`, `1.OA.D.8`) retain their deterministic lesson ids and may rebaseline by exactly one title/SortOrder position through a narrowly guarded accepted-history upgrade.

The eight Grade 1 fallback lessons for `CCSS:MP.1` through `CCSS:MP.8` also retain their deterministic lesson ids and titles. Because the Mathematical Practices apply across K–12 and are ordered after the Grade 1 numbered Standards, their Grade 1 fallback `SortOrder` values move forward by exactly one position (`21..28` to `22..29`). The accepted-history upgrade permits only that exact SortOrder transition with all other static lesson metadata unchanged.

Therefore the V13-to-V14 pedagogical transition affects exactly 13 existing Grade 1 lessons: five OA lessons with title/SortOrder rebaseline and eight Mathematical Practice lessons with SortOrder-only rebaseline.

Unrelated pedagogical lesson drift remains rejected.

The already accepted Grade 6, Grade 7 and Grade 8 pedagogical graphs are not rewritten by this repair and are verified after staging deployment.
