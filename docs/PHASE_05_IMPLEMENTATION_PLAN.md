# Phase 05 — School User Management

## Baseline

- Previous completed commit: `780ef04 feat: add school management`
- Stack: .NET 10 / ASP.NET Core MVC / Razor / EF Core / SQL Server / Identity / xUnit
- Tenant boundary: School
- Languages: English and Polish

## Scope

Phase 05 implements school-user lifecycle management.

Supported tenant roles:

- SchoolAdmin
- SubjectSupervisor
- Teacher
- Student

Features:

- list users for a school
- create a school user
- assign exactly one tenant role
- view user details
- activate/deactivate
- lock/unlock
- change role
- create initial password-setup link
- generate a new password-reset/setup link
- secure school-user login eligibility
- school-user landing dashboard
- SchoolAdmin management of users in their own school
- SuperAdmin management of users in any selected school
- strict tenant isolation
- English/Polish localization
- responsive UI
- automated tests

## Data Model

No new persistent fields are required.

Existing ApplicationUser already has:

- Id
- SchoolId
- Email/UserName
- IsActive
- CreatedAtUtc
- UpdatedAtUtc
- Identity lockout fields
- Identity password/security fields

Existing Identity role tables already support role assignment.

Therefore:

**No EF migration is required in Phase 05.**

## Security Rules

- SuperAdmin has SchoolId = null.
- Tenant users must have one SchoolId.
- Tenant users must have exactly one tenant role.
- SuperAdmin cannot be assigned through School User Management.
- SchoolAdmin can manage users only in their own school.
- Cross-school access is denied in the service layer and persistence layer.
- SchoolAdmin cannot deactivate, lock, reset, or change their own role through the admin UI.
- Archived schools are read-only.
- School users cannot sign in when their school is Suspended or Archived.
- Inactive or locked users cannot use school routes.
- State-changing actions are POST + anti-forgery.
- No public registration.
- Passwords and password-reset tokens are never logged.
- New accounts are created without a password.
- An Identity password-reset token is used as the initial setup link.
- Generating a new setup link removes the current password and invalidates active sessions.
- Strong password policy is enforced.

## Architecture

Core:

- business-neutral user persistence contracts
- repository abstraction

Services:

- user-management business rules
- tenant-scope enforcement
- sign-in eligibility

Data:

- ASP.NET Core Identity repository implementation

Web:

- authorization policies
- thin MVC controllers
- view models
- localized Razor screens

No MVC controller accesses DbContext.

## Routes

User management:

- GET  /School/Users
- GET  /School/Users/Create
- POST /School/Users/Create
- GET  /School/Users/{id}
- POST /School/Users/{id}/Active
- POST /School/Users/{id}/Lock
- POST /School/Users/{id}/Role
- POST /School/Users/{id}/Password-Link

Password setup:

- GET  /account/set-password
- POST /account/set-password

School landing:

- GET /school/dashboard

SuperAdmin selects a target school with `schoolId`.

SchoolAdmin resolves automatically to their own SchoolId.

## Tests

- SuperAdmin creation of tenant users
- prohibited SuperAdmin tenant-role assignment
- SchoolAdmin own-tenant access
- cross-school isolation
- self-management protection
- archived-school protection
- active-school sign-in
- suspended-school sign-in denial
- Identity repository creation
- role persistence
- persistence tenant boundary
- authorization attributes
- anti-forgery attributes
- localization parity
- required localization keys

## Manual Acceptance

Verify English and Polish:

- user list
- create
- details
- role change
- deactivate/activate
- lock/unlock
- password link
- initial password setup
- school-user login
- SchoolAdmin own-school access
- cross-school denial

Responsive widths:

- 320
- 375
- 480
- 768
- 1024
- 1280
- 1440+

No commit until manual verification passes.
