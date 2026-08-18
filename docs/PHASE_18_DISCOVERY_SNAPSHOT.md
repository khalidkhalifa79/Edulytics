# Phase 18 Discovery Snapshot

Generated: 2026-08-18T12:24:33Z

branch=phase18-audit-compliance
head=7941289f622b1d30d1dda717ddbb34d7094fdda1

## Existing audit/correlation code
```text
src/Edulytics.Core/Entities/OutboxMessage.cs:19:    public string CorrelationId { get; set; } = string.Empty;
src/Edulytics.Core/Entities/OutboxRequeueAudit.cs:3:public sealed class OutboxRequeueAudit
src/Edulytics.Core/Interfaces/IOutboxRepository.cs:13:    string CorrelationId,
src/Edulytics.Data/Configurations/OutboxMessageConfiguration.cs:38:        builder.Property(x => x.CorrelationId)
src/Edulytics.Data/Configurations/OutboxMessageConfiguration.cs:53:        builder.HasIndex(x => x.CorrelationId)
src/Edulytics.Data/Configurations/OutboxRequeueAuditConfiguration.cs:7:public sealed class OutboxRequeueAuditConfiguration
src/Edulytics.Data/Configurations/OutboxRequeueAuditConfiguration.cs:8:    : IEntityTypeConfiguration<OutboxRequeueAudit>
src/Edulytics.Data/Configurations/OutboxRequeueAuditConfiguration.cs:11:        EntityTypeBuilder<OutboxRequeueAudit> builder)
src/Edulytics.Data/Configurations/OutboxRequeueAuditConfiguration.cs:13:        builder.ToTable("OutboxRequeueAudits");
src/Edulytics.Data/Contexts/EdulyticsDbContext.cs:42:    public DbSet<OutboxRequeueAudit> OutboxRequeueAudits => Set<OutboxRequeueAudit>();
src/Edulytics.Data/Contexts/EdulyticsDbContext.cs:128:        builder.ApplyConfiguration(new OutboxRequeueAuditConfiguration());
src/Edulytics.Data/Migrations/20260816174615_Phase13PostgreSqlBaseline.Designer.cs:773:                    b.Property<string>("CorrelationId")
src/Edulytics.Data/Migrations/20260816174615_Phase13PostgreSqlBaseline.Designer.cs:810:                    b.HasIndex("CorrelationId")
src/Edulytics.Data/Migrations/20260816174615_Phase13PostgreSqlBaseline.cs:196:                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
src/Edulytics.Data/Migrations/20260816174615_Phase13PostgreSqlBaseline.cs:1437:                name: "IX_OutboxMessages_CorrelationId",
src/Edulytics.Data/Migrations/20260816174615_Phase13PostgreSqlBaseline.cs:1439:                column: "CorrelationId",
src/Edulytics.Data/Migrations/20260816231818_Phase14BackendResilience.Designer.cs:833:                    b.Property<string>("CorrelationId")
src/Edulytics.Data/Migrations/20260816231818_Phase14BackendResilience.Designer.cs:870:                    b.HasIndex("CorrelationId")
src/Edulytics.Data/Migrations/20260816234016_Phase15OutboxV2.Designer.cs:887:                    b.Property<string>("CorrelationId")
src/Edulytics.Data/Migrations/20260816234016_Phase15OutboxV2.Designer.cs:942:                    b.HasIndex("CorrelationId")
src/Edulytics.Data/Migrations/20260816234016_Phase15OutboxV2.Designer.cs:954:            modelBuilder.Entity("Edulytics.Core.Entities.OutboxRequeueAudit", b =>
src/Edulytics.Data/Migrations/20260816234016_Phase15OutboxV2.Designer.cs:983:                    b.ToTable("OutboxRequeueAudits", (string)null);
src/Edulytics.Data/Migrations/20260816234016_Phase15OutboxV2.Designer.cs:2047:            modelBuilder.Entity("Edulytics.Core.Entities.OutboxRequeueAudit", b =>
src/Edulytics.Data/Migrations/20260816234016_Phase15OutboxV2.cs:80:                name: "OutboxRequeueAudits",
src/Edulytics.Data/Migrations/20260816234016_Phase15OutboxV2.cs:92:                    table.PrimaryKey("PK_OutboxRequeueAudits", x => x.Id);
src/Edulytics.Data/Migrations/20260816234016_Phase15OutboxV2.cs:94:                        name: "FK_OutboxRequeueAudits_OutboxMessages_OutboxMessageId",
src/Edulytics.Data/Migrations/20260816234016_Phase15OutboxV2.cs:122:                name: "IX_OutboxRequeueAudits_ActorUserId_RequeuedAtUtc",
src/Edulytics.Data/Migrations/20260816234016_Phase15OutboxV2.cs:123:                table: "OutboxRequeueAudits",
src/Edulytics.Data/Migrations/20260816234016_Phase15OutboxV2.cs:127:                name: "IX_OutboxRequeueAudits_OutboxMessageId_RequeuedAtUtc",
src/Edulytics.Data/Migrations/20260816234016_Phase15OutboxV2.cs:128:                table: "OutboxRequeueAudits",
src/Edulytics.Data/Migrations/20260816234016_Phase15OutboxV2.cs:145:                name: "OutboxRequeueAudits");
src/Edulytics.Data/Migrations/20260817115720_Phase17PersistDataProtectionKeys.Designer.cs:887:                    b.Property<string>("CorrelationId")
src/Edulytics.Data/Migrations/20260817115720_Phase17PersistDataProtectionKeys.Designer.cs:942:                    b.HasIndex("CorrelationId")
src/Edulytics.Data/Migrations/20260817115720_Phase17PersistDataProtectionKeys.Designer.cs:954:            modelBuilder.Entity("Edulytics.Core.Entities.OutboxRequeueAudit", b =>
src/Edulytics.Data/Migrations/20260817115720_Phase17PersistDataProtectionKeys.Designer.cs:983:                    b.ToTable("OutboxRequeueAudits", (string)null);
src/Edulytics.Data/Migrations/20260817115720_Phase17PersistDataProtectionKeys.Designer.cs:2066:            modelBuilder.Entity("Edulytics.Core.Entities.OutboxRequeueAudit", b =>
src/Edulytics.Data/Migrations/EdulyticsDbContextModelSnapshot.cs:884:                    b.Property<string>("CorrelationId")
src/Edulytics.Data/Migrations/EdulyticsDbContextModelSnapshot.cs:939:                    b.HasIndex("CorrelationId")
src/Edulytics.Data/Migrations/EdulyticsDbContextModelSnapshot.cs:951:            modelBuilder.Entity("Edulytics.Core.Entities.OutboxRequeueAudit", b =>
src/Edulytics.Data/Migrations/EdulyticsDbContextModelSnapshot.cs:980:                    b.ToTable("OutboxRequeueAudits", (string)null);
src/Edulytics.Data/Migrations/EdulyticsDbContextModelSnapshot.cs:2063:            modelBuilder.Entity("Edulytics.Core.Entities.OutboxRequeueAudit", b =>
src/Edulytics.Data/Repositories/OutboxRepository.cs:136:                        row.CorrelationId,
src/Edulytics.Data/Repositories/OutboxRepository.cs:363:        _db.OutboxRequeueAudits.Add(
src/Edulytics.Data/Repositories/OutboxRepository.cs:364:            new OutboxRequeueAudit
src/Edulytics.Services/Assessments/AssessmentService.cs:769:                CorrelationId =
src/Edulytics.Services/Imports/ImportPlanBuilder.cs:115:                CorrelationId =
src/Edulytics.Web/Controllers/SystemStatusController.cs:64:                    CorrelationIdMiddleware
src/Edulytics.Web/Controllers/SystemStatusController.cs:65:                        .GetCorrelationId(
src/Edulytics.Web/Middleware/CorrelationIdMiddleware.cs:5:public sealed class CorrelationIdMiddleware
src/Edulytics.Web/Middleware/CorrelationIdMiddleware.cs:11:        "Edulytics.CorrelationId";
src/Edulytics.Web/Middleware/CorrelationIdMiddleware.cs:18:        CorrelationIdMiddleware> _logger;
src/Edulytics.Web/Middleware/CorrelationIdMiddleware.cs:20:    public CorrelationIdMiddleware(
src/Edulytics.Web/Middleware/CorrelationIdMiddleware.cs:22:        ILogger<CorrelationIdMiddleware> logger)
src/Edulytics.Web/Middleware/CorrelationIdMiddleware.cs:42:            ResolveCorrelationId(
src/Edulytics.Web/Middleware/CorrelationIdMiddleware.cs:59:                    ["CorrelationId"] =
src/Edulytics.Web/Middleware/CorrelationIdMiddleware.cs:87:    public static string GetCorrelationId(
src/Edulytics.Web/Middleware/CorrelationIdMiddleware.cs:113:    private static string ResolveCorrelationId(
src/Edulytics.Web/Program.cs:194:    CorrelationIdMiddleware>();
tests/Edulytics.PostgresGate/Program.cs:310:        CorrelationId =
tests/Edulytics.Tests/Phase10/RealtimeModelTests.cs:37:                                OutboxMessage.CorrelationId)
tests/Edulytics.Tests/Phase11/ImportPlanBuilderTests.cs:233:            message.CorrelationId);
tests/Edulytics.Tests/Phase12/ProductionHardeningTests.cs:42:            "CorrelationIdMiddleware",
tests/Edulytics.Tests/Phase12/ProductionHardeningTests.cs:187:            CorrelationIdMiddleware
tests/Edulytics.Tests/Phase12/ProductionHardeningTests.cs:194:            new CorrelationIdMiddleware(
tests/Edulytics.Tests/Phase12/ProductionHardeningTests.cs:202:                    CorrelationIdMiddleware>
tests/Edulytics.Tests/Phase12/ProductionHardeningTests.cs:218:                CorrelationIdMiddleware
tests/Edulytics.Tests/Phase12/ProductionHardeningTests.cs:230:            CorrelationIdMiddleware
tests/Edulytics.Tests/Phase12/ProductionHardeningTests.cs:235:            new CorrelationIdMiddleware(
tests/Edulytics.Tests/Phase12/ProductionHardeningTests.cs:239:                    CorrelationIdMiddleware>
```

## Current DbSets
```text
20:    public DbSet<School> Schools => Set<School>();
21:    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
22:    public DbSet<Term> Terms => Set<Term>();
23:    public DbSet<GradeLevel> GradeLevels => Set<GradeLevel>();
24:    public DbSet<ClassGroup> ClassGroups => Set<ClassGroup>();
25:    public DbSet<Subject> Subjects => Set<Subject>();
26:    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
27:    public DbSet<TeacherAssignment> TeacherAssignments => Set<TeacherAssignment>();
28:    public DbSet<StudentEnrollment> StudentEnrollments => Set<StudentEnrollment>();
29:    public DbSet<CurriculumTopic> CurriculumTopics => Set<CurriculumTopic>();
30:    public DbSet<LearningOutcome> LearningOutcomes => Set<LearningOutcome>();
31:    public DbSet<Assessment> Assessments => Set<Assessment>();
32:    public DbSet<AssessmentQuestion> AssessmentQuestions => Set<AssessmentQuestion>();
33:    public DbSet<QuestionLearningOutcome> QuestionLearningOutcomes => Set<QuestionLearningOutcome>();
34:    public DbSet<AssessmentResult> AssessmentResults => Set<AssessmentResult>();
35:    public DbSet<StudentAnswer> StudentAnswers => Set<StudentAnswer>();
36:    public DbSet<StudentOutcomeMastery> StudentOutcomeMasteries => Set<StudentOutcomeMastery>();
37:    public DbSet<ClassOutcomeSummary> ClassOutcomeSummaries => Set<ClassOutcomeSummary>();
38:    public DbSet<ClassTopicSummary> ClassTopicSummaries => Set<ClassTopicSummary>();
39:    public DbSet<ClassAssessmentTrend> ClassAssessmentTrends => Set<ClassAssessmentTrend>();
40:    public DbSet<SchoolAnalyticsSnapshot> SchoolAnalyticsSnapshots => Set<SchoolAnalyticsSnapshot>();
41:    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
42:    public DbSet<OutboxRequeueAudit> OutboxRequeueAudits => Set<OutboxRequeueAudit>();
43:    public DbSet<AnalyticsRefreshState> AnalyticsRefreshStates => Set<AnalyticsRefreshState>();
44:    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
45:    public DbSet<ImportValidationError> ImportValidationErrors => Set<ImportValidationError>();
46:    public DbSet<CurriculumFramework> CurriculumFrameworks => Set<CurriculumFramework>();
47:    public DbSet<CurriculumFrameworkVersion> CurriculumFrameworkVersions => Set<CurriculumFrameworkVersion>();
48:    public DbSet<SchoolCurriculumAdoption> SchoolCurriculumAdoptions => Set<SchoolCurriculumAdoption>();
49:    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
50:    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
```
