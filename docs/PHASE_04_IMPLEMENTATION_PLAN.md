# Phase 04 — School Management

## Baseline

`a236d4c feat: add secure language entry`

## Scope

Phase 04 implements SuperAdmin school management:

- school list
- create school
- school details
- edit core school information
- suspend school
- reactivate school
- archive school
- normalized unique school code
- optimistic concurrency
- English and Polish localization
- responsive UI
- automated tests

## Architecture

MVC controllers do not use `EdulyticsDbContext`.

Flow:

`SchoolsController`
→ `ISchoolManagementService`
→ `ISchoolRepository`
→ EF Core repository
→ `EdulyticsDbContext`

Business rules remain in the service layer.

## Database

No schema change is required.

The Phase 02 School model already contains:

- `NormalizedSchoolCode`
- `UpdatedAtUtc`
- `ArchivedAtUtc`
- `RowVersion`
- unique normalized school-code index

No Phase 04 migration should be created.

## School-code rules

- required
- trim
- uppercase normalization
- maximum length follows the existing EF limit
- allowed characters: `A-Z`, `0-9`, `-`
- unique by `NormalizedSchoolCode`
- immutable after creation

## Status transitions

Allowed:

- Active → Suspended
- Active → Archived
- Suspended → Active
- Suspended → Archived

Archived is terminal in the current workflow.

No hard delete is implemented.

## Authorization

All School Management routes require:

`PlatformAdministration`

State-changing operations use POST with anti-forgery validation.

## Localization

English and Polish resources have matching keys.

All School Management labels, errors, status labels, actions,
success messages and confirmation messages are localized.

## Responsive acceptance

Manual verification is required at:

- 320px
- 375px
- 480px
- 768px
- 1024px
- 1280px
- 1440px+

No horizontal scrolling, clipped text, overlap, hidden primary
actions or unreadable validation messages is acceptable.

## Verification before commit

- `dotnet build`
- `dotnet test`
- vulnerable package check
- `git diff --check`
- manual English UI verification
- manual Polish UI verification

Commit and push only after manual UI acceptance.
