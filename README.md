# Edulytics

Edulytics is a multi-tenant school learning analytics platform built with
.NET 10, ASP.NET Core MVC/Razor, Entity Framework Core, ASP.NET Core Identity,
SignalR, PostgreSQL/Npgsql and a durable Outbox architecture.

The current production-like staging topology uses Render and Neon PostgreSQL.
Production go-live belongs to a later production phase and is not inferred
merely from staging availability.

## Architecture

```text
Edulytics.Core
      ↓
Edulytics.Services
      ↓
Edulytics.Data
      ↓
Edulytics.Web

PostgreSQL / Neon = durable business truth
Outbox            = durable event intent
SignalR           = realtime invalidation transport
```

Controllers remain thin. Business rules belong in Services. Persistence
belongs in Data/repositories. Razor views do not contain SQL or domain logic.

## Main product domains

- schools and school users;
- academic structure;
- curriculum, topics and learning outcomes;
- assessments and results;
- analytics;
- imports;
- durable audit;
- SubjectSupervisor scope;
- reports and exports;
- notifications and connector delivery;
- operational administration;
- security, privacy and retention.

## Roles

The role model includes:

- `SuperAdmin` — platform scope;
- `SchoolAdmin` — one-school administrative scope;
- `SubjectSupervisor` — assigned subject scope;
- `Teacher` — assigned teaching scope;
- `Student` — student scope where product flow permits it.

Authorization is enforced server-side. Tenant checks remain authoritative even
when navigation hides unavailable actions.

## Prerequisites

- .NET 10 SDK;
- PostgreSQL 17-compatible environment for integration/database work;
- Docker for container/PostgreSQL acceptance workflows;
- Git;
- GitHub CLI for protected delivery workflows where applicable.

## Configuration and secrets

Do not commit credentials.

Runtime database configuration:

```text
ConnectionStrings__DefaultConnection
```

Migration/admin database configuration:

```text
ConnectionStrings__MigrationConnection
```

For Neon, runtime uses the approved pooled endpoint and schema migration uses
the direct migration endpoint.

Provider credentials such as email delivery secrets are environment
configuration and remain outside Git.

## Restore, build and test

```bash
dotnet restore Edulytics.sln
dotnet build Edulytics.sln --no-restore
dotnet test Edulytics.sln --no-build --no-restore
```

CI treats first-party compiler warnings as errors.

## Run locally

Configure a development PostgreSQL connection, then:

```bash
dotnet run --project src/Edulytics.Web/Edulytics.Web.csproj
```

## Database migrations

Check model consistency:

```bash
dotnet ef migrations has-pending-model-changes \
  --project src/Edulytics.Data/Edulytics.Data.csproj \
  --startup-project src/Edulytics.Web/Edulytics.Web.csproj \
  --context EdulyticsDbContext
```

Use repository migration workflows and a direct migration credential for schema
changes. Do not run production migrations with the normal runtime credential.

## Localization

Supported UI languages:

- English (`en`);
- Polish (`pl`).

Resource parity is a protected CI contract.

## Health

```text
GET /health/live
GET /health/ready
```

Public health output is intentionally minimal. Operational detail belongs in
authorized operations and monitoring surfaces.

## Important verification scripts

```text
scripts/ci-architecture-gate.py
scripts/ci-dependency-gate.sh
scripts/ci-localization-parity.py
scripts/ci-tenant-idor-gate.sh
scripts/verify-phase13.sh
scripts/verify-phase14.sh
scripts/verify-phase15.sh
scripts/verify-phase16.sh
scripts/verify-phase17.sh
scripts/verify-phase23-security.sh
scripts/test-domain.sh
```

The protected GitHub workflow also performs PostgreSQL, quality, SAST and
container/security checks.

## Frontend dependencies

Browser libraries are vendored deliberately. Exact versions and update policy:

```text
docs/FRONTEND_VENDORING.md
```

## Browser artifacts

Generated screenshots, browser ZIPs and similar evidence belong in CI
artifacts or temporary acceptance directories, not normal source commits.

The already-tracked historical `phase08-screenshots.zip` is preserved in this
phase. Phase 24 prevents new artifacts; it does not rewrite repository history.

## Deployment and operations

See:

- `docs/PRODUCTION_DEPLOYMENT.md`
- `docs/MONITORING_RUNBOOK.md`
- `docs/BACKUP_RESTORE_RUNBOOK.md`
- `docs/DEPENDENCY_GOVERNANCE.md`

Staging:

```text
https://staging.edulytiks.com
```

## Current engineering status

Phases through **Phase 27** are accepted and closed for application
production-readiness scope.

Phase 27 was qualified on the approved no-cost environment: Render Free staging
plus the existing Neon environment. The final application domain is
`https://edulytiks.com`.

Actual paid production provisioning/cutover is intentionally deferred until the
program and all required tests are complete. At that point, follow
`docs/ORACLE_PRODUCTION_HANDOFF.md`.

**Phase 28 is not started.** It is a post-real-go-live DR/security/capacity
review and begins only after the future Oracle production cutover.

## Phase 27.5 — Mathematics Curriculum Packs

Phase 27 remains closed. Phase 27.5 implements the Mathematics curriculum-pack layer for exactly four framework families: England/British, American/Common Core, UAE Ministry of Education, and Polish National Curriculum.

The phase records official-source provenance, native academic-level/pathway mappings, reuse/attribution contracts, runtime official-source verification, and the standards-linked unit/lesson blueprint layer.

Common Core commercial-use evidence is recorded as product-owner confirmed without inventing a licence number or effective date.

See:
- `docs/PHASE_27_5_MATHEMATICS_CURRICULUM_PACKS.md`
- `docs/curriculum/OFFICIAL_SOURCE_REGISTRY.md`

No Oracle provisioning, paid Render production, DNS cutover or Phase 28 work is performed by Phase 27.5.
