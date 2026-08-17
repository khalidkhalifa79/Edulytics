# Edulytics — Phase 16 CI/CD and Automated Quality/Security Gates

## Baseline

`771ad42d6dc5282055b7a36322cfb643b7ac20f8`

## Goal

Make production-quality requirements executable and fail-closed on every
change before later staging/deployment phases.

## Required gates implemented in this phase

- GitHub Actions on `push`, `pull_request` and manual dispatch;
- deterministic .NET SDK/tool versions;
- restore/build/full regression;
- coverage baseline and regression gate;
- EN/PL localization resource parity;
- architecture dependency boundaries;
- tenant/IDOR regression suite;
- NuGet dependency vulnerability audit;
- Gitleaks repository-history secret scan;
- CodeQL C# SAST;
- PostgreSQL 17 integration service;
- EF migration apply + pending-model validation;
- real PostgreSQL Outbox v2 concurrency gate;
- CI Docker image build;
- Trivy HIGH/CRITICAL fixed-vulnerability image gate;
- immutable GHCR image tag using exact Git commit SHA;
- test/migration/security/container artifact preservation;
- main branch required-check protection;
- automated deliberately-failing PR proof.

## Required checks

The workflow publishes four stable required checks:

- `phase16-quality`
- `phase16-postgres`
- `phase16-container`
- `phase16-sast`

Branch protection is configured only after the Phase 16 commit itself passes
all four checks.

## Coverage policy

The Phase 16 runner measures the current accepted full-suite Cobertura result
and records a repository baseline rounded down to one decimal point. Future CI
must not drop below the recorded line or branch baseline and must not reduce
the full test count below the measured baseline.

This is a regression floor, not a claim that current coverage is the final
target. Coverage growth is expected in later product phases.

## Tenant / IDOR gate

CI explicitly runs the accepted authorization/tenant suites independently of
the full regression:

- Phase 04 school authorization;
- Phase 05 school-user authorization;
- Phase 05 acceptance coverage including own-school management boundaries.

The full regression remains mandatory as well.

## PostgreSQL gate

A dedicated console gate runs only against PostgreSQL and proves:

- migrations apply;
- two concurrent Outbox workers do not double-claim one durable message;
- expired lease reclaim changes the lease token;
- stale completion is rejected;
- current lease completion succeeds;
- analytics refresh queue remains single-flight per school.

This prevents EF InMemory from being mistaken for the PostgreSQL integration
gate.

## Docker boundary

Phase 16 adds a CI build image so build/scan/SHA-image requirements are
enforceable now. Phase 17 still owns production deployment Docker/runtime
topology, Render/Neon staging, persistent Data Protection and public staging
acceptance.

## Secret and scanner versions

- Gitleaks: `v8.30.1`
- Trivy: `0.72.0`
- CodeQL Action: `v4`
- checkout: `v7`
- setup-dotnet: `v6`
- upload-artifact: `v7`

No scanner finding is suppressed by changing exit codes in the workflow.

## Merge gate proof

After the Phase 16 commit passes all required checks, the acceptance runner:

1. enables required checks on `main` and enforces them for administrators;
2. creates a temporary branch/PR containing `.ci/force-ci-failure`;
3. the `phase16-quality` job intentionally fails;
4. the PR must report a blocked/unstable merge state;
5. the proof PR and branch are closed/deleted.

The deliberate-failure file is never merged into `main`.

## Exit gate

Phase 16 is accepted only when a deliberately failing change is proven unable
to satisfy the protected main-branch merge requirements.
