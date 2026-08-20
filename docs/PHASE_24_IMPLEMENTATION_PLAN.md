# Phase 24 — Maintainability and Repository Hygiene

## Objective

Reduce maintenance cost without changing product behavior.

## Scope

- remove unused template files;
- overhaul README;
- split large services only where behavioral coverage supports extraction;
- clean repeated Razor markup through safe partials;
- organize CSS without accidental cascade changes;
- remove dependency on preview C# language mode unless proven required;
- enforce compiler warnings in CI;
- provide domain-oriented test categories while preserving phase evidence;
- prevent future screenshot/browser ZIP artifacts from source commits;
- document exact vendored frontend versions and update governance.

## Non-goals

Phase 24 does not implement multi-instance behavior, performance tuning or new
product functionality. Those belong to Phase 25+.

## Refactor rule

No large service, view or CSS refactor is accepted before:

1. a green behavioral baseline;
2. measured inventory;
3. behavioral coverage around the extraction boundary;
4. full regression after the change.

## Delivery rule

Protected `main` remains mandatory:

```text
feature branch
→ local acceptance
→ push
→ PR
→ required CI
→ protected merge
→ post-merge CI
→ staging regression where UI assets changed
```
