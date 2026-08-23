using System.Security.Cryptography;
using Edulytics.Core.Entities;
using Edulytics.Data.Configurations;
using Edulytics.Data.Identity;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Contexts;

public class EdulyticsDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>,
      IDataProtectionKeyContext
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
    public DbSet<SubjectSupervisorAssignment> SubjectSupervisorAssignments => Set<SubjectSupervisorAssignment>();
    public DbSet<ReportExportJob> ReportExportJobs => Set<ReportExportJob>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<NotificationDeliveryJob> NotificationDeliveryJobs => Set<NotificationDeliveryJob>();
    public DbSet<StudentEnrollment> StudentEnrollments => Set<StudentEnrollment>();
    public DbSet<CurriculumTopic> CurriculumTopics => Set<CurriculumTopic>();
    public DbSet<LearningOutcome> LearningOutcomes => Set<LearningOutcome>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<AssessmentQuestion> AssessmentQuestions => Set<AssessmentQuestion>();
    public DbSet<QuestionLearningOutcome> QuestionLearningOutcomes => Set<QuestionLearningOutcome>();
    public DbSet<AssessmentResult> AssessmentResults => Set<AssessmentResult>();
    public DbSet<StudentAnswer> StudentAnswers => Set<StudentAnswer>();
    public DbSet<StudentOutcomeMastery> StudentOutcomeMasteries => Set<StudentOutcomeMastery>();
    public DbSet<ClassOutcomeSummary> ClassOutcomeSummaries => Set<ClassOutcomeSummary>();
    public DbSet<ClassTopicSummary> ClassTopicSummaries => Set<ClassTopicSummary>();
    public DbSet<ClassAssessmentTrend> ClassAssessmentTrends => Set<ClassAssessmentTrend>();
    public DbSet<SchoolAnalyticsSnapshot> SchoolAnalyticsSnapshots => Set<SchoolAnalyticsSnapshot>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<OutboxRequeueAudit> OutboxRequeueAudits => Set<OutboxRequeueAudit>();
    public DbSet<AnalyticsRefreshState> AnalyticsRefreshStates => Set<AnalyticsRefreshState>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<ImportValidationError> ImportValidationErrors => Set<ImportValidationError>();
    public DbSet<CurriculumFramework> CurriculumFrameworks => Set<CurriculumFramework>();
    public DbSet<CurriculumFrameworkVersion> CurriculumFrameworkVersions => Set<CurriculumFrameworkVersion>();
    public DbSet<SchoolCurriculumAdoption> SchoolCurriculumAdoptions => Set<SchoolCurriculumAdoption>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<DemoRequest> DemoRequests => Set<DemoRequest>();
    public DbSet<DemoAccess> DemoAccesses => Set<DemoAccess>();
    public DbSet<SchoolSubscription> SchoolSubscriptions => Set<SchoolSubscription>();
    public DbSet<SubscriptionSeatChange> SubscriptionSeatChanges => Set<SubscriptionSeatChange>();
    public DbSet<SchoolBillingProfile> SchoolBillingProfiles => Set<SchoolBillingProfile>();
    public DbSet<BillingInvoice> BillingInvoices => Set<BillingInvoice>();
    public DbSet<BillingInvoiceLine> BillingInvoiceLines => Set<BillingInvoiceLine>();
    public DbSet<BankTransferPayment> BankTransferPayments => Set<BankTransferPayment>();
    public DbSet<BillingRefund> BillingRefunds => Set<BillingRefund>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public override int SaveChanges(
        bool acceptAllChangesOnSuccess)
    {
        PrepareApplicationManagedConcurrencyTokens();
        EnforceAuditAppendOnly();

        return base.SaveChanges(
            acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        PrepareApplicationManagedConcurrencyTokens();
        EnforceAuditAppendOnly();

        return base.SaveChangesAsync(
            acceptAllChangesOnSuccess,
            cancellationToken);
    }

    private void EnforceAuditAppendOnly()
    {
        foreach (var entry in
                 ChangeTracker.Entries<AuditLog>())
        {
            if (entry.State is
                EntityState.Modified or
                EntityState.Deleted)
            {
                throw new InvalidOperationException(
                    "AuditLog entries are append-only.");
            }
        }
    }

    private void PrepareApplicationManagedConcurrencyTokens()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Added &&
                entry.State != EntityState.Modified)
            {
                continue;
            }

            var property =
                entry.Metadata.FindProperty(
                    "RowVersion");

            if (property is null ||
                property.ClrType != typeof(byte[]) ||
                !property.IsConcurrencyToken)
            {
                continue;
            }

            entry.Property(
                    "RowVersion")
                .CurrentValue =
                RandomNumberGenerator.GetBytes(
                    16);
        }
    }

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
        builder.ApplyConfiguration(new SubjectSupervisorAssignmentConfiguration());
        builder.ApplyConfiguration(new ReportExportJobConfiguration());
        builder.ApplyConfiguration(new UserNotificationConfiguration());
        builder.ApplyConfiguration(new NotificationDeliveryJobConfiguration());
        builder.ApplyConfiguration(new StudentEnrollmentConfiguration());
        builder.ApplyConfiguration(new CurriculumTopicConfiguration());
        builder.ApplyConfiguration(new LearningOutcomeConfiguration());
        builder.ApplyConfiguration(new AssessmentConfiguration());
        builder.ApplyConfiguration(new AssessmentQuestionConfiguration());
        builder.ApplyConfiguration(new QuestionLearningOutcomeConfiguration());
        builder.ApplyConfiguration(new AssessmentResultConfiguration());
        builder.ApplyConfiguration(new StudentAnswerConfiguration());
        builder.ApplyConfiguration(new StudentOutcomeMasteryConfiguration());
        builder.ApplyConfiguration(new ClassOutcomeSummaryConfiguration());
        builder.ApplyConfiguration(new ClassTopicSummaryConfiguration());
        builder.ApplyConfiguration(new ClassAssessmentTrendConfiguration());
        builder.ApplyConfiguration(new SchoolAnalyticsSnapshotConfiguration());
        builder.ApplyConfiguration(new AuditLogConfiguration());
        builder.ApplyConfiguration(new OutboxMessageConfiguration());
        builder.ApplyConfiguration(new OutboxRequeueAuditConfiguration());
        builder.ApplyConfiguration(new AnalyticsRefreshStateConfiguration());
        builder.ApplyConfiguration(new ImportBatchConfiguration());
        builder.ApplyConfiguration(new ImportValidationErrorConfiguration());
        builder.ApplyConfiguration(new CurriculumFrameworkConfiguration());
        builder.ApplyConfiguration(new CurriculumFrameworkVersionConfiguration());
        builder.ApplyConfiguration(new SchoolCurriculumAdoptionConfiguration());
        builder.ApplyConfiguration(new IdempotencyRecordConfiguration());
        builder.ApplyConfiguration(new DemoRequestConfiguration());
        builder.ApplyConfiguration(new DemoAccessConfiguration());
        builder.ApplyConfiguration(new SchoolSubscriptionConfiguration());
        builder.ApplyConfiguration(new SubscriptionSeatChangeConfiguration());
        builder.ApplyConfiguration(new SchoolBillingProfileConfiguration());
        builder.ApplyConfiguration(new BillingInvoiceConfiguration());
        builder.ApplyConfiguration(new BillingInvoiceLineConfiguration());
        builder.ApplyConfiguration(new BankTransferPaymentConfiguration());
        builder.ApplyConfiguration(new BillingRefundConfiguration());

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("AspNetRoles");
        });
    }
}
