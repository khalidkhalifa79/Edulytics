# Phase 29 — Common Core High School Blueprint V2

## Locked production graph

- Official High School standards: **156**
- High School blueprints: **9**
- Units: **31**
- Lessons: **405**
- Formal mappings: **764**
- Traditional: **376 lessons / 721 mappings**
- Advanced: **29 lessons / 43 mappings**
- Advanced evidence:
  **35 primary explicit + 8 verified-content**
- Integrated pathway: **blocked**
- EF migration: **none**

## Backward compatibility

The accepted K-8 Common Core graph remains eight Schema V1
blueprints and is byte-preserved.

High School is represented by nine Schema V2 course
blueprints. Existing K-8 regression tests explicitly scope
their cardinality assertions to Schema V1; High School V2 has
its own independent graph tests.

Schema V2 source-license validation operates on the `Sources`
collection because a High School course may have multiple
approved pedagogical evidence sources.

## Immutable locks

Blueprint V2 contract:
`6f4824a7cd69a4ac9d3fb83a429bc31d593997e3308587552d21c575e3770e4e`

Advanced sequence:
`47ca026006b781d53fa9c6f980f9e54f03146df4bfbef23411fe26430f4b9baa`

No official Common Core V14 curriculum-pack node or link is
modified.
