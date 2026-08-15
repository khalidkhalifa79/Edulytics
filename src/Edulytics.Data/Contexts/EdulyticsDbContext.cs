using Edulytics.Core.Entities;
using Edulytics.Data.Configurations;
using Edulytics.Data.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Contexts;

public class EdulyticsDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public EdulyticsDbContext(DbContextOptions<EdulyticsDbContext> options)
        : base(options)
    {
    }

    public DbSet<School> Schools => Set<School>();
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<Term> Terms => Set<Term>();
    public DbSet<GradeLevel> GradeLevels => Set<GradeLevel>();
    public DbSet<ClassGroup> ClassGroups => Set<ClassGroup>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<TeacherAssignment> TeacherAssignments => Set<TeacherAssignment>();
    public DbSet<StudentEnrollment> StudentEnrollments => Set<StudentEnrollment>();
    public DbSet<CurriculumTopic> CurriculumTopics => Set<CurriculumTopic>();
    public DbSet<LearningOutcome> LearningOutcomes => Set<LearningOutcome>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new SchoolConfiguration());
        builder.ApplyConfiguration(new ApplicationUserConfiguration());
        builder.ApplyConfiguration(new AcademicYearConfiguration());
        builder.ApplyConfiguration(new TermConfiguration());
        builder.ApplyConfiguration(new GradeLevelConfiguration());
        builder.ApplyConfiguration(new ClassGroupConfiguration());
        builder.ApplyConfiguration(new SubjectConfiguration());
        builder.ApplyConfiguration(new StudentProfileConfiguration());
        builder.ApplyConfiguration(new TeacherAssignmentConfiguration());
        builder.ApplyConfiguration(new StudentEnrollmentConfiguration());
        builder.ApplyConfiguration(new CurriculumTopicConfiguration());
        builder.ApplyConfiguration(new LearningOutcomeConfiguration());

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("AspNetRoles");
        });
    }
}
