# Edulytics — Retired SQL Server Migration History

The SQL Server migration chain was accepted through Phase 11 and is
permanently recoverable from Git history.

Final SQL Server baseline commit:

`f19985f4ee66d11aa014110ae4b8916d64176730`

Historical migration sequence:

1. `20260812152209_InitialIdentityTenantFoundation`
2. `20260814222029_Phase06AcademicStructure`
3. `20260815045413_Phase07CurriculumLearningOutcomes`
4. `20260815065156_Phase075CurriculumFrameworkFoundation`
5. `20260815101210_Phase08AssessmentsAndResults`
6. `20260815203044_Phase09Analytics`
7. `20260815210442_Phase10RealtimeOutbox`
8. `20260815215753_Phase11DataImport`

Phase 12 did not add a schema migration.

These migrations are SQL Server-provider migrations and must not be
executed against PostgreSQL or Neon.

Phase 13 replaces the active migration chain with a fresh PostgreSQL
baseline because Edulytics had not yet launched against a production
database at the time of provider cutover.

This document is historical evidence only.
