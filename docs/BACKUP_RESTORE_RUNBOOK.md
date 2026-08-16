# Edulytics SQL Server Backup and Restore Runbook

## Objective

Production data must be recoverable independently of the application
deployment.

A backup is not considered proven until a restore rehearsal succeeds.

## Backup strategy

Use:

- scheduled full backups;
- differential backups where appropriate;
- transaction-log backups when required by the selected recovery model and
  recovery-point objective;
- backup storage outside the primary SQL Server host;
- access control on backup storage;
- backup-job monitoring;
- periodic restore rehearsals.

## Recovery targets

Final RPO and RTO values require business approval.

An initial technical operating target may be:

Target RPO: 15 minutes when transaction-log backups are configured.

Target RTO: 4 hours.

These values are targets, not guarantees.

## Before high-risk deployment

1. Confirm the latest scheduled backup succeeded.
2. Confirm required backup media is accessible.
3. Verify the backup.
4. Take an additional deployment backup where appropriate.

## Restore rehearsal

Always restore to a separate temporary database.

Never overwrite the live database during a rehearsal.

Verify:

- the restored database opens;
- __EFMigrationsHistory is readable;
- the latest expected migration exists;
- Schools exists;
- AssessmentResults exists;
- OutboxMessages exists;
- ImportBatches exists.

Remove the rehearsal database after validation.

## Recovery sequence

1. Stop or isolate application writes.
2. Preserve incident logs.
3. Choose the required recovery point.
4. Restore the full backup.
5. Apply differential backup if used.
6. Apply transaction-log backups in order if used.
7. Recover the database.
8. Verify migration history.
9. Verify critical tables.
10. Start one application instance.
11. Verify /health/live.
12. Verify /health/ready.
13. Perform targeted business-data checks.
14. Reopen traffic.
15. Document actual RPO and RTO.

## Security

Never commit database passwords to backup scripts.

Use deployment secrets.

Backup files must be accessible only to authorized operators and systems.
