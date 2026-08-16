# Edulytics — Phase 13 PostgreSQL / Npgsql / Neon Cutover

## Baseline

`f19985f4ee66d11aa014110ae4b8916d64176730`

Accepted Phase 12 production-hardening baseline.

## Objective

Move the authoritative Edulytics relational database platform from
Microsoft SQL Server to PostgreSQL.

Target production database:

- PostgreSQL;
- Neon managed PostgreSQL;
- Entity Framework Core;
- Npgsql.EntityFrameworkCore.PostgreSQL;
- ASP.NET Core remains the business/backend authority.

Prisma is not introduced.

## Provider

Old:

`Microsoft.EntityFrameworkCore.SqlServer`

New:

`Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3`

The application uses `UseNpgsql`.

## Connection policy

No database credentials are committed.

Runtime configuration:

`ConnectionStrings__DefaultConnection`

Production migration/admin access will use a separate direct Neon
connection and separate migration credential.

The production runtime may use a Neon pooled endpoint after validation.

## Concurrency decision

The existing application exposes `byte[] RowVersion` through domain,
service, repository and UI contracts.

Phase 13 deliberately preserves that public contract.

SQL Server database-generated `rowversion` is replaced by a
provider-independent application-managed concurrency token.

Rules:

- RowVersion remains `byte[]`;
- RowVersion remains an EF Core concurrency token;
- it is not database-generated;
- every Added or Modified tracked RowVersion entity receives a fresh
  cryptographically random 16-byte token before SaveChanges;
- stale writes continue to produce DbUpdateConcurrencyException;
- repository expected-RowVersion behavior remains intact;
- no silent last-write-wins conversion is accepted.

This minimizes provider-migration churn and remains compatible with
PostgreSQL.

## PostgreSQL unique/null semantics

PostgreSQL unique indexes normally treat NULL values as distinct.

Where the existing SQL Server model relied on one logical nullable value,
the PostgreSQL mapping explicitly uses Npgsql `AreNullsDistinct(false)`.

Critical scopes include:

- CurriculumFramework:
  OwnerSchoolId + NormalizedCode;

- SchoolCurriculumAdoption:
  SchoolId + AcademicYearId + GradeLevelId + SubjectId +
  FrameworkVersionId;

- primary SchoolCurriculumAdoption scope:
  SchoolId + AcademicYearId + GradeLevelId + SubjectId.

Filtered indexes are rewritten using PostgreSQL SQL syntax.

## JSON/text

Existing JSON payload strings remain text during this phase.

Phase 13 does not introduce jsonb merely because PostgreSQL supports it.

## Migrations

The eight accepted SQL Server migrations are historical and remain
recoverable from Git history at the Phase 12 baseline.

They are not valid PostgreSQL migrations.

After provider/model conversion, Phase 13 retires the active SQL Server
migration source and creates one fresh PostgreSQL baseline migration from
the complete accepted current model.

Production has not launched, so no production-data migration chain is
being broken.

## Runtime configuration precedence

`WebApplication.CreateBuilder` owns the standard ASP.NET Core
configuration provider ordering.

Edulytics must not append a second Development user-secrets provider
after the default builder configuration.

Database runtime configuration supplied through
`ConnectionStrings__DefaultConnection` must override Development
user-secrets values.

This is especially important during the SQL Server to PostgreSQL cutover:
a stale SQL Server development secret must never override an explicitly
supplied PostgreSQL runtime connection string.

## EF design-time operations

`EdulyticsDbContextFactory` is the authoritative design-time EF Core
factory.

It reads the PostgreSQL connection string only from:

- `ConnectionStrings__DefaultConnection`; or
- `EDULYTICS_CONNECTION_STRING`.

This prevents `dotnet ef` from depending on the ASP.NET Core startup
bootstrap lifecycle while generating or applying migrations.

No design-time credential is stored in source control.

## Validation order

1. exact accepted baseline;
2. provider replacement;
3. PostgreSQL model compatibility;
4. application-managed concurrency;
5. fresh PostgreSQL migration;
6. clean local PostgreSQL database;
7. migration apply;
8. schema inspection;
9. Phase 13 tests;
10. full regression;
11. readiness/runtime verification;
12. Neon non-production validation;
13. SQL Server-removal audit;
14. documentation;
15. final acceptance;
16. commit/push only after all gates pass.

## No provider retries yet

General write retry/idempotency policy is intentionally not added here.

It belongs to Phase 14, where retries, idempotency, timeouts and
backpressure are designed together.

## Phase 13 acceptance

Phase 13 cannot close until:

- build passes;
- full regression passes;
- PostgreSQL model tests pass;
- real PostgreSQL migration applies;
- optimistic concurrency is verified;
- Identity works against PostgreSQL;
- imports work against PostgreSQL;
- analytics works against PostgreSQL;
- Outbox works against PostgreSQL;
- health/readiness works;
- Neon non-production works;
- migration uses direct Neon connectivity;
- no active SQL Server runtime dependency remains;
- no secrets are committed.
