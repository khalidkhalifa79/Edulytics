# Edulytics PostgreSQL / Neon Backup and Restore Runbook

## Objective

Production data must be recoverable independently of the application deploy.

A production backup/recovery capability is not accepted merely because a
provider advertises retention. Before go-live, Edulytics requires evidence that
the production Neon branch has a usable recovery window/snapshot strategy and
that an isolated restore can be inspected safely.

## Active database architecture

Edulytics production uses PostgreSQL on Neon.

- normal application traffic uses the approved pooled runtime connection;
- EF Core migrations use a separate direct/non-pooler connection;
- application releases and database recovery are separate operational actions;
- SQL Server backup procedures are not part of the active architecture.

## Production recovery strategy

Use the recovery facilities enabled by the selected Neon production plan,
including branch history / point-in-time restore and/or snapshots where
available.

Before go-live record, outside Git secrets:

- Neon project and production branch identifiers;
- configured history/retention window;
- most recent usable recovery point or snapshot evidence;
- operator authorized to perform recovery;
- approved RPO and RTO targets.

Do not put database credentials in this document or in CI artifacts.

## Before every high-risk production deploy

1. Confirm `/health/ready` is healthy before deployment.
2. Confirm the production branch is inside a usable recovery window.
3. Record the UTC deployment start time.
4. Record the exact application release SHA/image digest.
5. Confirm the migration is backward-compatible with rollback policy.
6. Confirm the direct migrator credential is available to pre-deploy only.
7. Do not run destructive ad-hoc SQL as part of release preparation.

## Isolated restore rehearsal

A restore rehearsal must not overwrite the live production branch.

Restore or branch from an approved historical point into an isolated recovery
branch/environment, then verify:

- PostgreSQL accepts a connection;
- `__EFMigrationsHistory` is readable;
- the latest expected migration is present;
- `Schools` exists;
- `AssessmentResults` exists;
- `OutboxMessages` exists;
- `ImportBatches` exists;
- `DataProtectionKeys` exists;
- representative tenant-scoped reads are consistent.

The first real production restore exercise and measured RTO belong to Phase 28,
but Phase 27 must prove that backup/restore capability is configured and
available before accepting go-live.

## Emergency recovery sequence

1. Stop or isolate application writes.
2. Preserve Render/application/Neon incident evidence.
3. Record the active release SHA and migration version.
4. Select the required safe recovery point.
5. Restore to an isolated branch first when time and incident severity permit.
6. Validate critical schema/data.
7. Perform the provider-approved production branch restore/switchover.
8. Start one application instance.
9. Verify `/health/live`.
10. Verify `/health/ready`.
11. Verify authentication and tenant isolation.
12. Verify Outbox worker state/dead letters.
13. Verify representative business data.
14. Reopen traffic.
15. Record actual RPO/RTO and incident corrective actions.

## Security

- Never commit Neon passwords, connection strings, tokens, or snapshot secrets.
- Runtime and migration credentials remain separate.
- Recovery access is restricted to authorized operators.
- Do not paste production credentials into tickets, logs, screenshots, or
  repository documentation.
