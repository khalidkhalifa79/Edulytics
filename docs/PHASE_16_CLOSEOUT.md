# Edulytics — Phase 16 Closeout

## Baseline

`771ad42d6dc5282055b7a36322cfb643b7ac20f8`

## Automated gates introduced

### Quality

- exact .NET SDK/tool restoration;
- restore/build;
- full xUnit regression;
- full-suite Cobertura coverage with deterministic multi-shard merge;
- coverage/test-count regression baseline;
- EN/default ↔ PL resource parity;
- architecture dependency test;
- explicit tenant/IDOR regression test;
- Phase 15 Testing-host log-cleanliness gate;
- NuGet vulnerability audit;
- Gitleaks history scan;
- whitespace gate.

### PostgreSQL

GitHub Actions starts PostgreSQL 17 and performs:

- all EF migrations;
- pending-model check;
- idempotent migration-script generation;
- real PostgreSQL Outbox two-worker claim;
- lease-expiry reclaim;
- stale-owner fencing;
- analytics single-flight.

### SAST

CodeQL v4 runs C# `security-extended` analysis and uploads code-scanning
results plus retained SARIF artifacts.

### Container

CI builds `.ci/Dockerfile`, scans the resulting image with Trivy and rejects
fixed HIGH/CRITICAL vulnerabilities.

On `main`, the image is pushed only as:

`ghcr.io/khalidkhalifa79/edulytics:<exact-git-sha>`

No mutable `latest` release tag is created by Phase 16.

### Evidence preservation

GitHub Actions retains:

- TRX;
- Cobertura XML;
- full test log;
- tenant/IDOR TRX files;
- dependency/secret scan logs;
- idempotent migration SQL;
- PostgreSQL gate output;
- image metadata;
- Trivy JSON;
- CodeQL SARIF.

## Branch policy

After the Phase 16 commit itself passes CI, the acceptance runner configures
`main` required status checks and administrator enforcement for:

- `phase16-quality`
- `phase16-postgres`
- `phase16-container`
- `phase16-sast`

The runner then creates a temporary failing PR using
`.ci/force-ci-failure`. Phase 16 is accepted only if GitHub reports the
required quality check failed and the PR merge state is blocked/unstable.
The proof PR and branch are deleted afterwards.

## Important workflow change after Phase 16

Once branch protection is enabled, later phases must not assume they can push
untested commits directly to protected `main`. Later one-command phase runners
must work through a temporary implementation branch/PR, wait for required
checks, merge only after green CI, then verify `main`.

## Phase boundary

Phase 17 owns actual Render + Neon staging deployment, production-like Docker
runtime acceptance, TLS/domain, persistent Data Protection, SMTP sandbox,
restart/rollback and staging smoke. Phase 16 establishes the automated gates
that Phase 17 must pass.


## Remote CI setup repair

The first pushed Phase 16 workflow reached GitHub Actions but failed during
job setup before repository code executed. A non-idempotent local normalization
step had compounded otherwise valid action tags into invalid refs such as
`v7.0.1.0.1`.

The repair normalizes action references by complete token pattern instead of
prefix replacement, validates each referenced action definition against GitHub
before push, and runs actionlint before the repair commit is created.

No product, database, migration, authorization, coverage, Docker, or security
gate behavior was weakened by this repair.
