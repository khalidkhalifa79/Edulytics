using Edulytics.Core.Interfaces;
using Edulytics.Core.Onboarding;
using Edulytics.Data.Contexts;
using Edulytics.Data.Repositories;
using Edulytics.Services.Auditing;
using Edulytics.Services.Onboarding;
using Edulytics.Web.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Edulytics.Tests.Phase25B;

public sealed class Phase25BAuditGuaranteeTests
{
    [Fact]
    public async Task AuditService_RecordAsync_PersistsAuditLog()
    {
        var options =
            new DbContextOptionsBuilder<EdulyticsDbContext>()
                .UseInMemoryDatabase(
                    $"phase25b-audit-{Guid.NewGuid():N}")
                .Options;

        await using var db =
            new EdulyticsDbContext(options);

        var repository =
            new AuditRepository(db);

        var service =
            new AuditService(
                repository,
                new FixedAuditMetadataProvider());

        var schoolId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        await service.RecordAsync(
            new AuditEvent(
                SchoolId: schoolId,
                Action: "DemoAccess.Granted",
                EntityType: "DemoAccess",
                EntityId: entityId.ToString("D"),
                Feature: "CustomerOnboarding",
                NewValues:
                    new Dictionary<string, object?>
                    {
                        ["durationDays"] = 7
                    },
                ResultSummary:
                    "Phase25B audit guarantee test."));

        var row =
            await db.AuditLogs.SingleAsync();

        Assert.Equal(
            schoolId,
            row.SchoolId);

        Assert.Equal(
            "DemoAccess.Granted",
            row.Action);

        Assert.Equal(
            "DemoAccess",
            row.EntityType);

        Assert.Equal(
            entityId.ToString("D"),
            row.EntityId);

        Assert.Equal(
            "CustomerOnboarding",
            row.Feature);
    }

    [Fact]
    public void ProductionOnboardingRegistration_RequiresAuditDependency()
    {
        var services =
            new ServiceCollection();

        services.AddCustomerOnboardingPhase25B();

        services.AddScoped<
            ICustomerOnboardingRepository,
            ThrowIfUsedOnboardingRepository>();

        var audit =
            new Phase25BTestAuditService();

        services.AddScoped<IAuditService>(
            _ => audit);

        using var provider =
            services.BuildServiceProvider();

        using var scope =
            provider.CreateScope();

        var resolved =
            scope.ServiceProvider
                .GetRequiredService<
                    ICustomerOnboardingService>();

        var concrete =
            Assert.IsType<
                CustomerOnboardingService>(
                    resolved);

        var constructor =
            Assert.Single(
                typeof(CustomerOnboardingService)
                    .GetConstructors());

        var parameters =
            constructor.GetParameters();

        Assert.Equal(
            typeof(IAuditService),
            parameters[1].ParameterType);

        Assert.False(
            parameters[1].IsOptional);

        Assert.False(
            parameters[1].HasDefaultValue);

        Assert.NotNull(concrete);
    }

    private sealed class FixedAuditMetadataProvider
        : IAuditRequestMetadataProvider
    {
        public AuditRequestMetadata GetCurrent() =>
            new(
                ActorUserId: null,
                ActorRole: "SuperAdmin",
                CorrelationId:
                    "phase25b-audit-guarantee",
                IpAddress: "127.0.0.1",
                UserAgent:
                    "Phase25BAuditGuaranteeTests",
                Source: "Test");
    }

    private sealed class ThrowIfUsedOnboardingRepository
        : ICustomerOnboardingRepository
    {
        private static Exception NotUsed() =>
            new InvalidOperationException(
                "Repository behavior is not used by "
                + "the DI composition test.");

        public Task<bool>
            ExistsOpenByNormalizedEmailAsync(
                string normalizedEmail,
                CancellationToken cancellationToken =
                    default) =>
            throw NotUsed();

        public Task<IReadOnlyList<
            Edulytics.Core.Entities.DemoRequest>>
            ListRequestsAsync(
                CancellationToken cancellationToken =
                    default) =>
            throw NotUsed();

        public Task<
            Edulytics.Core.Entities.DemoRequest?>
            GetRequestAsync(
                Guid requestId,
                CancellationToken cancellationToken =
                    default) =>
            throw NotUsed();

        public Task<
            Edulytics.Core.Entities.DemoAccess?>
            GetDemoAccessByRequestAsync(
                Guid requestId,
                CancellationToken cancellationToken =
                    default) =>
            throw NotUsed();

        public Task<
            Edulytics.Core.Entities.DemoAccess?>
            GetDemoAccessBySchoolAsync(
                Guid schoolId,
                CancellationToken cancellationToken =
                    default) =>
            throw NotUsed();

        public Task<CustomerOnboardingWriteResult>
            AddRequestAsync(
                Edulytics.Core.Entities.DemoRequest request,
                CancellationToken cancellationToken =
                    default) =>
            throw NotUsed();

        public Task<CustomerOnboardingWriteResult>
            SaveRequestAsync(
                Edulytics.Core.Entities.DemoRequest request,
                byte[] expectedRowVersion,
                CancellationToken cancellationToken =
                    default) =>
            throw NotUsed();

        public Task<CustomerOnboardingWriteResult>
            SaveDemoAccessAsync(
                Edulytics.Core.Entities.DemoAccess access,
                byte[] expectedRowVersion,
                CancellationToken cancellationToken =
                    default) =>
            throw NotUsed();

        public Task<CustomerOnboardingProvisionResult>
            CreateDemoAsync(
                Guid requestId,
                byte[] expectedRequestRowVersion,
                DateTime startsAtUtc,
                DateTime expiresAtUtc,
                CancellationToken cancellationToken =
                    default) =>
            throw NotUsed();

        public Task<CustomerOnboardingProvisionResult>
            ProvisionCustomerAsync(
                Guid requestId,
                byte[] expectedRequestRowVersion,
                string normalizedSchoolCode,
                string defaultCulture,
                string timeZoneId,
                DateTime utcNow,
                CancellationToken cancellationToken =
                    default) =>
            throw NotUsed();
    }
}
