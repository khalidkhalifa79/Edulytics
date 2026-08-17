# Phase 17 — Staging Runbook

## Neon

Use a dedicated Edulytics staging database/branch.

Do not reuse another product database.

Configure two connections:

- `ConnectionStrings__DefaultConnection`
  - pooled Neon runtime endpoint
- `ConnectionStrings__MigrationConnection`
  - direct Neon migration endpoint

The Render pre-deploy process exposes the direct migration connection to
the EF design-time context as well as passing it to `efbundle --connection`.
This is required because Edulytics intentionally uses an
`IDesignTimeDbContextFactory` that refuses to construct a PostgreSQL context
without an explicit connection.

Never commit either value.

## Render

Create the staging service from `render.yaml`.

The service must use the repository Dockerfile and `/health/ready`.

The pre-deploy migration bundle must complete before the new application
revision receives traffic.

## SMTP

Use a sandbox/test provider.

Set the `Email__Smtp__*` environment variables only in Render.

Do not use production recipients during staging acceptance.

## Live acceptance

Required evidence:

1. Render build PASS.
2. Pre-deploy EF migration PASS.
3. `/health/live` healthy.
4. `/health/ready` healthy.
5. Authentication works.
6. EN and PL work.
7. SignalR connects.
8. Outbox worker remains healthy.
9. A safe staging mutation persists in Neon.
10. Restart returns to ready state.
11. Authentication/Data Protection survives restart as designed.
12. Rollback to the previous Render revision succeeds.
13. Custom staging domain HTTPS succeeds.
14. No secrets appear in logs.

Do not call Phase 17 complete from repository tests alone.


## Data Protection certificate

Staging must provide:

- `Edulytics__Hosting__DataProtectionCertificateBase64`
- `Edulytics__Hosting__DataProtectionCertificatePassword`

The first value is a Base64-encoded PKCS#12/PFX certificate containing
a private key. Keep both values exclusively in Render secrets.

Production-mode startup intentionally fails when the certificate is required
but missing.
