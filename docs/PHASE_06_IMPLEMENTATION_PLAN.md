# Edulytics — Phase 06 Implementation Plan

## Baseline
`0b778b2 feat: complete school user management`

## Scope
Phase 06 builds school-scoped AcademicYear, Term, GradeLevel, ClassGroup,
Subject, TeacherAssignment and student enrollment.

Student enrollment uses StudentProfile, which is defined by the authoritative
data model and is required by future AssessmentResult.StudentProfileId.
StudentProfile.UserId remains optional.

## Authorization and tenant rules
- School is the tenant boundary.
- SchoolAdmin administers academic structure for the actor's own active school.
- SubjectSupervisor, Teacher and Student cannot administer academic structure.
- Academic relationships are school-scoped.
- Teacher assignments require an active same-school Teacher account.
- Optional StudentProfile.UserId requires an active same-school Student account.
- Suspended/archived schools cannot mutate academic structure.

## Data model
- AcademicYear: Id, SchoolId, Name, StartsOn, EndsOn, Status,
  CreatedAtUtc, UpdatedAtUtc, RowVersion.
- Term: Id, SchoolId, AcademicYearId, Name, StartsOn, EndsOn, Status.
- GradeLevel: Id, SchoolId, Name, Order.
- ClassGroup: Id, SchoolId, AcademicYearId, GradeLevelId, Name, Code,
  NormalizedCode, Status, RowVersion.
- Subject: Id, SchoolId, Name, Code, NormalizedCode, Status, RowVersion.
- StudentProfile: Id, SchoolId, UserId nullable, StudentNumber,
  NormalizedStudentNumber, FirstName, LastName, DisplayName, Status,
  CreatedAtUtc, UpdatedAtUtc.
- TeacherAssignment: Id, SchoolId, TeacherUserId, ClassGroupId, SubjectId,
  AcademicYearId, CreatedAtUtc.
- StudentEnrollment: Id, SchoolId, StudentProfileId, ClassGroupId,
  AcademicYearId, EnrolledAtUtc.

## Validation
- Academic year Start < End.
- Term is inside its academic year.
- Grade order > 0.
- Codes normalize to uppercase and allow A-Z, 0-9 and hyphen.
- Uniqueness rules are tenant-scoped.
- One enrollment per student profile per academic year.
- RowVersion protects AcademicYear, ClassGroup and Subject edits.

## UI
Responsive Academic Structure workspace plus edit screens for AcademicYear,
ClassGroup and Subject. All state changes use POST + anti-forgery.

## Localization
English and Polish only, including feedback, validation and accessibility text.

## Migration
One reviewed Phase 06 migration. DropTable/DropColumn is not accepted.

## Verification
Build, Phase 06 tests, full regression, migration inspection, SQL Server
database update, architecture/security guards, localization parity,
whitespace check, then manual EN/PL and responsive browser verification.
