# Phase 17 — Docker + Render + Neon Staging Environment

## Baseline

`dbc7628c494f86d1ad2a49658c374c8eb902a8e4`

Phase 16 CI/CD is already accepted and protected `main` requires its
quality gates.

## Goal

Create a production-like staging deployment contract without changing
Edulytics business behavior or creating a second source of truth.

## Scope

- Production multi-stage Docker image.
- Non-root ASP.NET Core runtime.
- Render Web Service Blueprint.
- Render readiness check at `/health/ready`.
- Graceful shutdown window.
- Neon pooled connection for runtime.
- Separate Neon direct connection for migrations.
- EF migration bundle executed as Render pre-deploy work.
- PostgreSQL-backed ASP.NET Core Data Protection key persistence.
- X.509 protection for persisted Data Protection key material.
- Explicit trusted-forwarded-header deployment switch.
- SMTP sandbox configuration surface.
- Docker/PostgreSQL production-mode smoke test.
- Restart acceptance test.
- Phase 16 quality gates remain mandatory.

## Data Model Delta

Phase 17 adds the ASP.NET Core Data Protection key store to the existing
PostgreSQL database.

No school, user, academic, curriculum, assessment, analytics, import,
Outbox or idempotency business schema is redesigned in this phase.

## Security

- No credentials are committed.
- Runtime and migration database connection strings remain environment
  secrets.
- The migration secret is separate from the runtime connection.
- Staging uses a distinct Data Protection application name.
- Container runs as the built-in non-root `app` user.
- `main` is not pushed directly.

## External Gates

The repository cannot invent external credentials.

Before live staging acceptance, configure:

1. A dedicated Edulytics Neon project/branch.
2. Pooled staging runtime connection string.
3. Direct staging migration connection string.
4. Render staging service from `render.yaml`.
5. Staging SMTP sandbox credentials.
6. Custom staging domain and TLS.
7. Live SignalR, Outbox, restart and rollback evidence.

Phase 17 must not be marked ACCEPTED until those live staging gates pass.
