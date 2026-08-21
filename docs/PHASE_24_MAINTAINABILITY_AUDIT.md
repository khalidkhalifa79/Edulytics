# Phase 24 — Maintainability Audit

## Baseline

`33e446f0e379ae3b05a4daedc21135306c7bc6a8`

## Initial findings

The Phase 24 baseline was green before refactoring.

Deterministic hygiene findings:

- three template `Class1.cs` files were still tracked;
- empty template `UnitTest1.cs` was still tracked;
- README still described SQL Server;
- repository language mode was explicitly `preview`;
- warnings were not enforced as errors;
- a historical `phase08-screenshots.zip` remains tracked;
- vendored frontend versions required explicit governance.

The historical screenshot ZIP is not deleted or history-rewritten by this
foundation step. New browser artifact ZIPs are blocked through `.gitignore`.

Large service/view/CSS candidates remain inventory until behavioral coverage is
reviewed. They will not be split merely because of line count.

## Raw baseline inventory

```text
PHASE24 BASELINE INVENTORY
baseline=33e446f0e379ae3b05a4daedc21135306c7bc6a8

===== TRACKED PLACEHOLDERS =====
TRACKED src/Edulytics.Core/Class1.cs
TRACKED src/Edulytics.Data/Class1.cs
TRACKED src/Edulytics.Services/Class1.cs
TRACKED tests/Edulytics.Tests/UnitTest1.cs

===== TOP SERVICE FILES BY LINES =====
 11397 total
  1348 src/Edulytics.Services/Assessments/AssessmentService.cs
  1140 src/Edulytics.Services/Academics/AcademicStructureService.cs
  1083 src/Edulytics.Services/Reports/ReportQueryService.cs
   971 src/Edulytics.Services/Users/SchoolUserManagementService.cs
   950 src/Edulytics.Services/Imports/DataImportService.cs
   821 src/Edulytics.Services/Curriculum/CurriculumService.cs
   717 src/Edulytics.Services/Analytics/AnalyticsService.cs
   574 src/Edulytics.Services/Notifications/NotificationService.cs
   563 src/Edulytics.Services/Schools/SchoolManagementService.cs
   547 src/Edulytics.Web/Background/OutboxProcessorBackgroundService.cs
   387 src/Edulytics.Services/SubjectSupervisors/SubjectSupervisorAssignmentService.cs
   366 src/Edulytics.Services/Reports/ReportExportService.cs
   270 src/Edulytics.Web/Background/AnalyticsRefreshBackgroundService.cs
   239 src/Edulytics.Web/Email/MailKitUserInvitationDeliveryService.cs
   235 src/Edulytics.Web/Operations/OperationalConsoleService.cs
   160 src/Edulytics.Services/Auditing/AuditService.cs
   159 src/Edulytics.Services/Realtime/RealtimeGroupService.cs
   145 src/Edulytics.Services/Auditing/AuditQueryService.cs
   110 src/Edulytics.Web/Privacy/SensitiveDataRetentionBackgroundService.cs

===== TOP RAZOR VIEWS BY LINES =====
  5513 total
   485 src/Edulytics.Web/Views/AcademicStructure/Index.cshtml
   472 src/Edulytics.Web/Views/Analytics/Index.cshtml
   425 src/Edulytics.Web/Views/Reports/Index.cshtml
   373 src/Edulytics.Web/Views/Operations/Index.cshtml
   285 src/Edulytics.Web/Views/Audit/Index.cshtml
   223 src/Edulytics.Web/Views/SchoolUsers/Details.cshtml
   209 src/Edulytics.Web/Views/Assessments/Details.cshtml
   206 src/Edulytics.Web/Views/Curriculum/Index.cshtml
   198 src/Edulytics.Web/Views/Imports/Index.cshtml
   195 src/Edulytics.Web/Views/Schools/Details.cshtml
   184 src/Edulytics.Web/Views/SubjectSupervisorAssignments/Index.cshtml
   176 src/Edulytics.Web/Views/SchoolHome/Dashboard.cshtml
   174 src/Edulytics.Web/Views/SchoolUsers/Index.cshtml
   168 src/Edulytics.Web/Views/Imports/Details.cshtml
   162 src/Edulytics.Web/Views/Schools/Index.cshtml
   132 src/Edulytics.Web/Views/Assessments/Results.cshtml
   126 src/Edulytics.Web/Views/Assessments/Index.cshtml
   123 src/Edulytics.Web/Views/Notifications/Index.cshtml
   108 src/Edulytics.Web/Views/Schools/Create.cshtml

===== CSS SIZE =====
3007 src/Edulytics.Web/wwwroot/css/site.css

===== CSS SECTION MARKERS =====
168:   Authentication
465:   Responsive
518:/* ===== PHASE 04 SCHOOL MANAGEMENT ===== */
1013:   Phase 05 — School User Management
1418:/* ===== Phase 06 — Academic Structure ===== */
1592:/* ===== Phase 07 — Curriculum and Learning Outcomes ===== */
1774:/* ===== Phase 08 — Assessments and Results ===== */
2055:   Phase 09 — Analytics
2836:/* PHASE 20 REPORTS */
2901:/* PHASE 21 NOTIFICATIONS */
2921:/* PHASE22_OPERATIONAL_CONSOLE */

===== TRACKED ZIP FILES =====
phase08-screenshots.zip

===== LANGUAGE / WARNING POLICY =====
3:    <TargetFramework>net10.0</TargetFramework>
6:    <LangVersion>preview</LangVersion>
7:    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>

===== README FIRST 30 LINES =====
# Edulytics
Production-ready multi-tenant school learning analytics platform built with ASP.NET Core MVC, EF Core, SQL Server, and Identity.

===== FRONTEND LIBRARIES =====
bootstrap
jquery
jquery-validation
jquery-validation-unobtrusive
signalr

```

## Foundation decisions

- unused templates: remove after proving no external references;
- README: update to PostgreSQL/Npgsql/Neon/Render;
- C# language mode: stable .NET 10 SDK default;
- warnings: errors in CI;
- historical phase tests: preserve;
- domain test access: add a developer-facing selector;
- browser evidence: CI/temp artifacts, not new source ZIPs;
- frontend vendor versions: document exact audited versions;
- CSS: preserve cascade until evidence-backed extraction.

## Refactor candidates

The measured candidates printed by the foundation run are reviewed before the
next implementation step.

## Selected behavior-preserving refactors

### AssessmentService

`AssessmentService.cs` was the largest measured service file at the Phase 24
baseline.

It also has dedicated Phase 08 behavioral tests, so it is the safest large
service candidate for structural cleanup.

The class is split as one partial service across cohesive files:

- `AssessmentService.cs` — constructor and read/query orchestration;
- `AssessmentService.Commands.cs` — mutation orchestration;
- `AssessmentService.Support.cs` — audit, scope/access, mapping and support
  helpers.

This intentionally does **not** change:

- `IAssessmentService`;
- dependency injection;
- repository contracts;
- authorization decisions;
- transaction/concurrency behavior;
- tenant boundaries;
- persistence semantics.

The purpose is navigation and responsibility grouping, not redesign.

### Analytics view

`Views/Analytics/Index.cshtml` was one of the largest Razor views.

The populated dashboard result sections are extracted to
`_AnalyticsDashboardResults.cshtml`. The parent view retains page-level status,
filtering, empty-state behavior and script loading.

This preserves the existing CSS classes, localization keys and SignalR script
placement while reducing the parent page size.

### CSS

No stylesheet split is performed in this refactor.

The existing 3007-line stylesheet has phase/feature delimiters. A mechanical
multi-file extraction would alter source-order/cascade risk without adding
equivalent value. Phase 24 therefore documents the organization and preserves
the current cascade.

### Phase 10 realtime architecture contract correction

The Phase 10 `ResultService_ProducesOutboxEvent` test originally read only
`AssessmentService.cs`.

After Phase 24 split the same `AssessmentService` partial class across query,
command and support files, the outbox behavior remained unchanged in
`AssessmentService.Commands.cs`, but the filename-coupled test produced a false
failure.

The Phase 10 contract now concatenates every `AssessmentService*.cs` file in
deterministic ordinal order before checking for:

- `AddOutboxAsync`;
- `AssessmentResultChangedEvent`;
- `AssessmentResultEntered`;
- `AssessmentResultUpdated`.

This changes no production behavior. It preserves the original architectural
assertion while allowing the behavior-preserving partial-class refactor.

## Staging acceptance correction — Schools navigation

During Phase 24 staging acceptance, the platform Schools index exposed school
management actions but did not provide an in-application path back to the
Platform dashboard.

The browser Back button was therefore the only obvious return path.

The corrective change reuses existing contracts:

- `PlatformController.Dashboard`;
- `PlatformResource.BackToDashboard`;
- `PlatformResource.pl.BackToDashboard`;
- `.school-back-link`.

No new localization key or CSS rule is introduced.

A Phase 24 source regression test now requires the Schools index to target
`Platform/Dashboard` with the localized `BackToDashboard` label.
