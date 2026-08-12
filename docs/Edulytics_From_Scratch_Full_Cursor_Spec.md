# Edulytics — From-Scratch Full Product and Technical Build Specification for Cursor AI

## Critical Instruction

This project must be built **from zero**.

Do not continue from any existing repository, existing commit, previous project state, previous database, previous migrations, or previous implementation.

Any instruction that says “continue from existing codebase”, “last completed commit”, “current phase”, “existing implementation”, or similar must be ignored.

Cursor AI must treat this file as the authoritative project description for creating a new clean production-ready project from scratch.

---

# 1. Project Name

**Edulytics**

The name combines:

- Education
- Analytics
- Learning intelligence
- School performance measurement

---

# 2. Product Vision

Edulytics is a production-grade, bilingual, multi-tenant learning analytics platform for schools.

The system helps schools collect, structure, analyze, and monitor educational performance data in real time.

It is not only a gradebook. It is a decision-support system for school leaders, teachers, subject supervisors, and eventually students.

The platform should help answer questions such as:

- Which students are falling behind?
- Which class is weak in a specific topic?
- Which learning outcomes are not mastered?
- Which teacher needs academic support?
- Which students need intervention?
- Which curriculum topics should be retaught?
- Which school has missing or poor-quality data?
- What changed today after teachers entered new results?
- Which dashboard values changed in real time?
- Which assessment looks unusual or inconsistent?
- Which interventions improved student performance?

The system must be designed as a real production system, not a prototype.

---

# 3. Initial Product Market

Initial target:

```text
Country/market: Poland
Initial subject: Grade 6 Mathematics
UI languages: English and Polish
School model: multi-tenant SaaS
```

The product should be expandable later to:

- more grades
- more subjects
- more countries
- more languages
- parent portal
- student portal
- AI recommendations
- mobile apps
- external integrations

Do not build those later features in the first implementation unless explicitly approved.

---

# 4. Core Product Concept

The school provides structured academic data:

- schools
- users
- academic years
- classes
- students
- teachers
- subjects
- curriculum topics
- learning outcomes
- assessments
- assessment results

Edulytics transforms that data into:

- student mastery profiles
- class performance dashboards
- topic weakness analysis
- subject-level reports
- school-level analytics
- real-time operational status
- academic risk indicators
- audit history
- import validation reports

The system must separate:

```text
Raw academic data
from
Calculated analytics / projections / dashboard snapshots
```

Raw data must remain preserved and auditable.

Analytics can be recalculated.

---

# 5. Technology Stack

Use:

- C#
- .NET 10
- ASP.NET Core MVC
- Razor Views
- SQL Server
- EF Core
- ASP.NET Core Identity
- xUnit
- PowerShell scripts
- Git
- SignalR later for real-time dashboards
- Background services later for async processing
- Structured logging
- Health checks later

Do not use unless explicitly approved later:

- ASPX
- Web Forms
- React
- Blazor
- Microservices
- Public registration
- Mobile apps
- Payments
- Parent portal
- AI tutoring
- Social login

---

# 6. Required Architecture

Use a **modular monolith**.

Recommended solution:

```text
Edulytics.sln
├─ src/
│  ├─ Edulytics.Core
│  ├─ Edulytics.Services
│  ├─ Edulytics.Data
│  └─ Edulytics.Web
├─ tests/
│  └─ Edulytics.Tests
├─ docs/
└─ scripts/
```

## 6.1 Project Responsibilities

### Edulytics.Core

Contains:

- entities
- enums
- constants
- domain interfaces
- domain contracts
- value objects
- result objects
- business-neutral abstractions

Must not reference:

- ASP.NET Core MVC
- EF Core implementation
- SQL Server
- Razor
- web-specific code

### Edulytics.Services

Contains:

- business services
- use cases
- validators
- tenant access rules
- school management logic
- user management logic
- academic structure logic
- assessment logic
- analytics orchestration logic
- audit orchestration
- import orchestration
- event publishing abstractions

Should reference:

```text
Core
```

Should not reference:

```text
Web
```

### Edulytics.Data

Contains:

- EF Core DbContext
- Identity persistence models
- EF Core configurations
- migrations
- persistence services
- database implementation details
- SQL Server configuration

Should reference:

```text
Core
```

May provide implementations registered for use by Services/Web, but controllers must not use DbContext directly.

### Edulytics.Web

Contains:

- ASP.NET Core MVC controllers
- Razor views
- view models
- localization resources
- web authorization policies
- filters/middleware
- CSS
- JavaScript if needed
- SignalR hubs later
- UI validation presentation

Should reference:

```text
Services
Data
Core if needed for constants/enums
```

---

# 7. Hard Engineering Rules

Cursor AI must obey these rules at all times:

- Build from a clean new project.
- Do not continue any old codebase.
- Do not use ASPX.
- Do not use Web Forms.
- Do not use React.
- Do not use Blazor.
- Do not use microservices.
- Do not put DbContext in MVC controllers.
- Do not write SQL in controllers.
- Do not write SQL in Razor views.
- Do not put business logic in Razor views.
- Controllers must stay thin.
- Controllers call services.
- Business rules live in services/domain code.
- Do not create public registration.
- Do not store credentials in code, docs, commits, screenshots, or chat.
- Do not create migrations blindly.
- Explain the data model before migrations.
- Run build and tests after implementation.
- Do not commit until verification is clean.
- Every UI page needs real design and responsive behavior.
- Every UI page must support English and Polish.

---

# 8. User Roles

## 8.1 SuperAdmin

Platform-level administrator.

Rules:

- Has `SchoolId = null`.
- Can manage schools.
- Can suspend/reactivate/archive schools.
- Can view platform dashboard.
- Can manage platform-level settings later.
- Cannot be created through public registration.
- Should be bootstrapped securely.

## 8.2 SchoolAdmin

School-level administrator.

Rules:

- Has exactly one `SchoolId`.
- Can manage users inside their school.
- Can configure academic structure.
- Can view school dashboards.
- Cannot access another school.
- Cannot create another school.

## 8.3 SubjectSupervisor

Academic supervisor.

Rules:

- Has exactly one `SchoolId`.
- Can oversee assigned subjects.
- Can view subject analytics.
- Can compare classes and topics.
- Cannot access another school.
- Cannot manage platform settings.

## 8.4 Teacher

Teacher user.

Rules:

- Has exactly one `SchoolId`.
- Can access assigned classes.
- Can enter or import assessment results.
- Can view class/student analytics for assigned classes.
- Cannot access unrelated classes.
- Cannot access another school.

## 8.5 Student

Learner.

Rules:

- Has exactly one `SchoolId`.
- Can only access own data if student portal is enabled later.
- Student portal is not required in the first MVP unless approved.

---

# 9. Tenant Model

`School` is the tenant boundary.

All school-owned data must be scoped by `SchoolId`.

Examples of school-scoped entities:

- School users
- Academic years
- Terms
- Grade levels
- Classes
- Subjects
- Students
- Teacher assignments
- Curriculum mappings
- Assessments
- Assessment results
- Analytics projections
- Imports
- Notifications
- Audit entries when related to school data

Rules:

```text
SuperAdmin → SchoolId = null
School users → SchoolId required
School data → SchoolId required
```

Tenant isolation must be enforced through:

- service layer
- authorization policies
- EF query strategy
- database constraints
- tests
- audit logs
- SignalR group isolation later

No school user must ever access another school’s data.

---

# 10. Language and Localization

Supported languages:

```text
English: en
Polish: pl
```

## 10.1 Initial Language Flow

```text
Language selector
→ User chooses Polski or English
→ Login page appears entirely in selected language
→ Application appears entirely in selected language
→ Sign out clears selected language
→ Return to language selector
```

## 10.2 Language Selector

The first page is intentionally simple:

```text
Edulytics logo/name

[ 🇵🇱 Polski ]
[ 🇬🇧 English ]
```

Rules:

- Two cards only.
- No long welcome text.
- No paragraph explanation.
- No mixed extra text.
- It is the only intentionally bilingual page.

## 10.3 Strict Localization Rule

After language selection, every visible and accessibility-facing string must use the chosen language only.

This includes:

- page titles
- headings
- subheadings
- labels
- buttons
- links
- placeholders
- validation messages
- login errors
- authorization errors
- alerts
- empty states
- status labels
- table headings
- modal text
- confirmation messages
- success messages
- ARIA labels
- screen-reader-only text
- tooltips

Polish page must not show English fallback text.

English page must not show Polish fallback text.

This is a functional requirement, not only a design requirement.

---

# 11. Responsive UI Requirements

Every page must work at:

```text
320px
375px
480px
768px
1024px
1280px
1440px+
```

A page is incomplete if it has:

- horizontal scrolling
- clipped text
- overlapping UI
- hidden primary action
- unreadable errors
- unusable forms
- broken mobile tables
- poor resizing behavior

Completion rule:

```text
Functionality
+ selected-language completeness
+ responsive behavior
+ validation/error states
+ manual browser verification
```

---

# 12. Main Domains of the Final Product

The complete system should eventually include:

1. Platform Administration
2. School Management
3. User and Role Management
4. Academic Years
5. Terms/Semesters
6. Grade Levels
7. Classes
8. Subjects
9. Teacher Assignments
10. Student Enrollment
11. Curriculum
12. Learning Outcomes
13. Assessments
14. Assessment Questions
15. Assessment Results
16. Student Answers
17. Mastery Analytics
18. Dashboards
19. Real-Time Updates
20. Data Imports
21. Audit Logs
22. Notifications
23. Reports/Exports
24. Health Monitoring
25. Operational Admin Tools

---

# 13. Suggested Initial Data Model

Do not implement everything at once. Start in phases.

## 13.1 School

Suggested production-ready fields:

```text
Id
Name
SchoolCode
NormalizedSchoolCode
Status
CountryCode
City
ContactEmail
DefaultCulture
TimeZoneId
CreatedAtUtc
UpdatedAtUtc
ArchivedAtUtc
RowVersion
```

Status:

```text
Active = 1
Suspended = 2
Archived = 3
```

Rules:

- Name required.
- SchoolCode required.
- NormalizedSchoolCode unique.
- SchoolCode normalized to uppercase.
- SchoolCode should allow A-Z, 0-9, hyphen.
- Archived schools are retained, not deleted.
- Use optimistic concurrency.

## 13.2 ApplicationUser

Extend ASP.NET Core Identity user.

Suggested fields:

```text
Id
SchoolId nullable
CreatedAtUtc
UpdatedAtUtc
IsActive
```

Rules:

- SuperAdmin has `SchoolId = null`.
- School users must have `SchoolId`.
- Email must be unique.
- No public registration.
- Role assignment controlled by admins.

## 13.3 AcademicYear

```text
Id
SchoolId
Name
StartsOn
EndsOn
Status
CreatedAtUtc
UpdatedAtUtc
RowVersion
```

## 13.4 Term

```text
Id
SchoolId
AcademicYearId
Name
StartsOn
EndsOn
Status
```

## 13.5 GradeLevel

```text
Id
SchoolId
Name
Order
```

## 13.6 ClassGroup

```text
Id
SchoolId
AcademicYearId
GradeLevelId
Name
Code
Status
```

## 13.7 Subject

```text
Id
SchoolId
Name
Code
Status
```

## 13.8 StudentProfile

```text
Id
SchoolId
UserId nullable
StudentNumber
FirstName
LastName
DisplayName
Status
CreatedAtUtc
UpdatedAtUtc
```

## 13.9 TeacherAssignment

```text
Id
SchoolId
TeacherUserId
ClassGroupId
SubjectId
AcademicYearId
```

## 13.10 CurriculumTopic

```text
Id
SchoolId nullable if platform template
SubjectId
GradeLevelId
Name
Order
```

## 13.11 LearningOutcome

```text
Id
SchoolId nullable if platform template
TopicId
Code
Description
Weight
Order
```

## 13.12 Assessment

```text
Id
SchoolId
SubjectId
ClassGroupId
AcademicYearId
TermId
Title
AssessmentDate
MaxScore
Status
CreatedByUserId
CreatedAtUtc
UpdatedAtUtc
RowVersion
```

## 13.13 AssessmentResult

```text
Id
SchoolId
AssessmentId
StudentProfileId
Score
Percentage
EnteredByUserId
EnteredAtUtc
UpdatedAtUtc
RowVersion
```

## 13.14 AuditLog

```text
Id
OccurredAtUtc
ActorUserId
ActorSchoolId nullable
TargetSchoolId nullable
Action
EntityType
EntityId
OldValuesJson
NewValuesJson
IpAddress
UserAgent
CorrelationId
```

## 13.15 OutboxMessage

For reliable async processing later.

```text
Id
EventType
PayloadJson
OccurredAtUtc
ProcessedAtUtc nullable
ProcessingAttempts
LastError
CorrelationId
SchoolId nullable
```

---

# 14. Real-Time and Concurrency Requirements

The system must be designed for real school usage where many users work at the same time.

Use cases:

- multiple teachers entering results concurrently
- multiple admins editing school/user data
- dashboards updating after new assessment results
- import batches running while users browse dashboards
- background analytics recalculation
- school status changes reflected quickly

## 14.1 Real-Time Design

Use SignalR later.

Suggested groups:

```text
platform:superadmins
school:{schoolId}:admins
school:{schoolId}:subject:{subjectId}:supervisors
school:{schoolId}:class:{classId}:teachers
student:{studentUserId}
```

Rules:

- Never broadcast school data to another school.
- Authorize SignalR hubs.
- Join groups based on claims and service-side validation.

## 14.2 Domain Events

Suggested events:

```text
SchoolCreated
SchoolStatusChanged
UserCreated
AcademicStructureChanged
AssessmentCreated
AssessmentResultEntered
AssessmentResultUpdated
StudentMasteryChanged
ImportBatchCompleted
DataQualityAlertCreated
```

## 14.3 Outbox Pattern

Use later when reliable events are required.

Purpose:

- save data and events in the same transaction
- process events safely
- retry failures
- avoid losing dashboard updates

## 14.4 Optimistic Concurrency

Use `RowVersion` for important editable records:

- School
- User profile
- AcademicYear
- ClassGroup
- Subject
- Assessment
- AssessmentResult

On conflict:

- do not overwrite silently
- show localized conflict message
- allow reload

## 14.5 Idempotency

Use idempotency for:

- imports
- background jobs
- event processing
- bulk operations
- notification sending

---

# 15. Analytics Requirements

Do not calculate everything directly from raw tables on every request.

Separate:

```text
Raw data
Calculated projections
Dashboard snapshots
```

Potential projection tables:

```text
StudentOutcomeMastery
ClassOutcomeSummary
SubjectTopicSummary
SchoolPerformanceSnapshot
TeacherClassSummary
DashboardMetricSnapshot
```

Mastery scale example:

```text
0–39: Critical gap
40–59: Weak
60–74: Developing
75–89: Secure
90–100: Strong
```

Analytics must be:

- school-scoped
- recalculable
- auditable where needed
- safe under concurrent updates
- efficient for dashboards

---

# 16. Data Import Requirements

Future import workflow:

```text
Upload
→ parse
→ validate
→ preview
→ confirm
→ apply transactionally
→ audit
→ update analytics
→ notify dashboards
```

Import types:

- students
- teachers
- classes
- subjects
- assessment results
- curriculum mappings

Validation:

- required columns
- invalid data types
- duplicate rows
- unknown students
- unknown class codes
- invalid score ranges
- cross-school references
- empty required fields
- conflicting existing records

Safety:

- do not partially apply silently
- store import batch
- preserve validation errors
- support retry safely
- protect tenant isolation

---

# 17. Security Requirements

- No public registration.
- Strong password policy.
- Lockout on failed login.
- Generic login failure messages.
- Anti-forgery on all state-changing POSTs.
- No state-changing GETs.
- Role-based and policy-based authorization.
- Tenant validation in services.
- Secure authentication cookies.
- No credentials in source.
- No cross-school leakage.
- Audit sensitive actions.

Sensitive actions to audit:

- school creation
- school status changes
- user creation
- role changes
- password reset
- imports
- assessment changes
- score changes
- data exports

---

# 18. Production Readiness

The final system should support:

- structured logging
- health checks
- readiness checks
- error pages
- correlation IDs
- configuration validation
- environment-specific settings
- database backup strategy
- deployment documentation
- rate limiting later
- security headers later
- monitoring later
- performance optimization

No secrets in source.

---

# 19. Testing Strategy

Use xUnit.

Test:

- domain rules
- validators
- school code normalization
- role constants
- authorization policies
- localization coverage
- service behavior
- tenant isolation
- EF configuration
- school status transitions
- login validation
- concurrency conflicts later
- import validation later

Before commit:

```text
dotnet build
dotnet test
git diff --check
manual browser verification
```

---

# 20. Implementation Phases From Scratch

## Phase 01 — Empty Solution Foundation

Build:

- solution
- project structure
- .NET SDK pinning
- Directory.Build.props
- initial build/test scripts
- initial xUnit project
- basic MVC app
- docs folder
- git initialization

Acceptance:

- build passes
- test command runs
- solution structure correct
- no database yet unless approved

## Phase 02 — Identity and Tenant Foundation

Build:

- EF Core setup
- SQL Server connection
- ApplicationUser
- ApplicationRole
- School entity
- role constants
- school status enum
- first migration
- database update script
- SuperAdmin bootstrap

Acceptance:

- database created
- roles created
- SuperAdmin created once
- second bootstrap does not create duplicate SuperAdmin
- tests pass

## Phase 03 — Secure Language Entry

Build:

- language selector
- culture cookie
- English/Polish resources
- login
- logout
- access denied
- initial protected dashboard
- authorization fallback
- PlatformAdministration policy
- responsive UI

Acceptance:

- language selector is two cards only
- Polish login has Polish only
- English login has English only
- validation messages localized
- logout clears language
- responsive verified

## Phase 04 — School Management

Build:

- school list
- create school
- details
- edit
- suspend
- reactivate
- archive
- unique school code
- localized validation
- responsive UI
- tests

## Phase 05 — School User Management

Build:

- create SchoolAdmin
- create Teacher
- create SubjectSupervisor
- create Student
- activate/deactivate users
- role assignment
- tenant isolation

## Phase 06 — Academic Structure

Build:

- academic years
- terms
- grades
- classes
- subjects
- teacher assignments
- student enrollment

## Phase 07 — Curriculum and Learning Outcomes

Build:

- Grade 6 Mathematics curriculum
- topics
- learning outcomes
- outcome codes
- outcome weighting

## Phase 08 — Assessments and Results

Build:

- assessments
- questions
- outcomes mapping
- result entry
- score validation

## Phase 09 — Analytics

Build:

- mastery calculations
- class heatmaps
- topic analysis
- risk indicators
- dashboard projections

## Phase 10 — Real-Time Dashboards

Build:

- SignalR
- school-scoped groups
- dashboard update events
- event/outbox foundation if needed

## Phase 11 — Data Import

Build:

- CSV/Excel upload
- validation preview
- confirm import
- import batch history
- analytics update

## Phase 12 — Production Hardening

Build:

- logs
- health checks
- error handling
- deployment docs
- monitoring
- backup/restore plan

---

# 21. Cursor Execution Rules

Cursor must:

1. Read this full specification.
2. Do not code immediately.
3. Produce a phase plan first.
4. Wait for approval.
5. Implement in controlled blocks.
6. Run build and tests.
7. Ask for manual UI verification when UI changes.
8. Commit only after clean verification.
9. Explain uncertainty clearly.
10. Communicate in Arabic unless asked otherwise.

---

# 22. First Cursor Prompt

Use this prompt after attaching this file:

```text
Read the attached Edulytics from-scratch specification fully.

We are building Edulytics from zero.

Do not continue any old repository, commit, database, or previous implementation.

Do not code yet.

First produce the complete Phase 01 implementation plan:
- solution structure
- projects
- dependencies
- packages
- scripts
- tests
- build/test commands
- git strategy
- risks
- acceptance criteria

Reply in Arabic.
```

---

# 23. Final Goal

Edulytics must become a real production-ready platform, not a fragile demo.

Priorities:

1. Correct architecture
2. Security
3. Tenant isolation
4. Localization completeness
5. Responsive UI
6. Data correctness
7. Concurrency safety
8. Real-time readiness
9. Auditability
10. Test coverage
11. Production readiness
