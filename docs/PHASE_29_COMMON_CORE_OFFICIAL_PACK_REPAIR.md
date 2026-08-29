# Phase 29 — Common Core Official Pack Integrity Repair

## Authoritative finding

The accepted `US-CCSS-MATH / CCSSM-2010` snapshot contained a systemic PDF parsing-boundary defect.

The authoritative ADA Common Core Mathematics source was verified with SHA-256:

`1dc360aa21390c2860c939f731b693295ee1537cbb2b2e3be2ccd06dcb06898c`

Full numbered-Standard audit:

- K–8 numbered Standards: `228`
- High School numbered Standards: `156`
- total numbered Standards: `384`
- Mathematical Practices: `8`
- corrected official count: `392`

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

## Corrected accepted fingerprint

- NodeCount: `458`
- OfficialNodeCount: `392`
- Domain nodes: `66`
- numbered Standards: `384`
- ContentDigest: `5cab24d5f9b3d27839207db19ca8182d69acca4c4e252e3d8da822cb64541e82`

The authoritative source identity did not change, so `SourceDigest` remains unchanged.

## Restored nodes

The repair adds exactly 38 nodes: 32 Standards and 6 Domains.

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
- the exact 38-node historical missing set;
- legacy ContentHash values for all 140 corrected existing Standards;
- SHA-256 of the accepted official text for every one of the 384 numbered Standards.

This allows automated regression checks without downloading the source PDF during normal CI/startup.

## Database repair safety

No database wipe is permitted.

No EF migration is required.

The startup seeder may upgrade an existing Common Core installation only when its import state exactly matches the historical accepted `420 / 360` fingerprint.

It then additionally requires:

- the exact 38-node missing set;
- no stale/unexpected node codes;
- deterministic ids for every existing node;
- unchanged static metadata for every existing node;
- for each text that requires repair, the exact historical ContentHash recorded in the integrity manifest.

Unrelated existing rows are not rewritten.

The operation remains fail-closed for any unrecognized drift.

On PostgreSQL the existing Phase 27.5 advisory-lock transaction wraps the repair.

## Pedagogical lesson preservation

All original 420 official-pack nodes retain their deterministic ids and SortOrder.

The 38 restored nodes use deterministic ids and append-only SortOrder values.

Therefore existing outcome-backed pedagogical lesson identities, titles and SortOrder values remain unchanged.

The restored Standards add new pedagogical lesson coverage only.

Source-driven Phase 29 lesson sequencing remains a separate pedagogical layer.
