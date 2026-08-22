using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Onboarding;
using Edulytics.Services.Onboarding;

namespace Edulytics.Tests.Phase25B;

public sealed class Phase25BServiceTests
{
    [Fact]
    public async Task PublicRequest_RequiresMinimum500AndConsent()
    {
        var repo = new FakeRepository();
        var service = new CustomerOnboardingService(repo, new Phase25BTestAuditService());
        var result = await service.SubmitDemoRequestAsync(new DemoRequestSubmission(
            "School", "Contact", "contact@example.com", null, "PL", "Warsaw", 499, null, false));
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, x => x.Code == OnboardingErrorCode.MinimumStudentCount);
        Assert.Contains(result.Errors, x => x.Code == OnboardingErrorCode.PrivacyConsentRequired);
        Assert.Empty(repo.Requests);
    }

    [Fact]
    public async Task DuplicateOpenEmail_IsGenericSuccessWithoutSecondLead()
    {
        var repo = new FakeRepository();
        repo.Requests.Add(NewRequest(DemoRequestStatus.New));
        var service = new CustomerOnboardingService(repo, new Phase25BTestAuditService());
        var result = await service.SubmitDemoRequestAsync(ValidSubmission());
        Assert.True(result.Succeeded);
        Assert.Single(repo.Requests);
    }

    [Fact]
    public async Task Pipeline_RejectsSkippingStages()
    {
        var repo = new FakeRepository();
        var lead = NewRequest(DemoRequestStatus.New);
        repo.Requests.Add(lead);
        var service = new CustomerOnboardingService(repo, new Phase25BTestAuditService());
        var result = await service.UpdateLeadAsync(
            lead.Id,
            DemoRequestStatus.Qualified,
            null,
            null,
            lead.RowVersion.ToArray());
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, x => x.Code == OnboardingErrorCode.InvalidTransition);
    }

    [Fact]
    public async Task DemoSchedule_NormalizesBrowserUnspecifiedDateTimeToUtc()
    {
        var repo = new FakeRepository();
        var lead = NewRequest(DemoRequestStatus.Contacted);
        repo.Requests.Add(lead);

        var service = new CustomerOnboardingService(repo, new Phase25BTestAuditService());

        var browserValue = DateTime.SpecifyKind(
            new DateTime(2026, 8, 22, 11, 31, 0),
            DateTimeKind.Unspecified);

        var result = await service.UpdateLeadAsync(
            lead.Id,
            DemoRequestStatus.DemoScheduled,
            browserValue,
            "UTC browser binding regression",
            lead.RowVersion.ToArray());

        Assert.True(result.Succeeded);
        Assert.Equal(
            DemoRequestStatus.DemoScheduled,
            lead.Status);
        Assert.True(
            lead.DemoScheduledAtUtc.HasValue);
        Assert.Equal(
            DateTimeKind.Utc,
            lead.DemoScheduledAtUtc.Value.Kind);
        Assert.Equal(
            new DateTime(
                2026,
                8,
                22,
                11,
                31,
                0,
                DateTimeKind.Utc),
            lead.DemoScheduledAtUtc.Value);
    }
    [Fact]
    public async Task GrantDemo_RecordsRequiredAuditEvent()
    {
        var repo = new FakeRepository();
        var lead = NewRequest(DemoRequestStatus.Qualified);
        repo.Requests.Add(lead);

        var audit = new Phase25BTestAuditService();
        var service =
            new CustomerOnboardingService(
                repo,
                audit);

        var result =
            await service.GrantDemoAsync(
                lead.Id,
                lead.RowVersion.ToArray());

        Assert.True(result.Succeeded);

        var recorded =
            Assert.Single(
                audit.Recorded,
                item =>
                    item.Action ==
                    "DemoAccess.Granted");

        Assert.Equal(
            "CustomerOnboarding",
            recorded.Feature);

        Assert.Equal(
            "DemoAccess",
            recorded.EntityType);

        Assert.Equal(
            lead.Id.ToString("D"),
            recorded.EntityId);

        Assert.NotNull(recorded.NewValues);

        Assert.Equal(
            7,
            Assert.IsType<int>(
                recorded.NewValues![
                    "durationDays"]));
    }



    [Fact]
    public async Task QualifiedLead_GrantDemo_IsSevenDays()
    {
        var repo = new FakeRepository();
        var lead = NewRequest(DemoRequestStatus.Qualified);
        repo.Requests.Add(lead);
        var service = new CustomerOnboardingService(repo, new Phase25BTestAuditService());
        var result = await service.GrantDemoAsync(lead.Id, lead.RowVersion.ToArray());
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Invitation);
        Assert.Equal(TimeSpan.FromDays(7), repo.LastDemoExpiry!.Value - repo.LastDemoStart!.Value);
    }

    [Fact]
    public async Task Provisioning_RequiresWon()
    {
        var repo = new FakeRepository();
        var lead = NewRequest(DemoRequestStatus.Qualified);
        repo.Requests.Add(lead);
        var service = new CustomerOnboardingService(repo, new Phase25BTestAuditService());
        var result = await service.ProvisionCustomerAsync(
            lead.Id,
            "SCH-001",
            "en",
            "Europe/Warsaw",
            lead.RowVersion.ToArray());
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, x => x.Code == OnboardingErrorCode.ProvisionRequiresWon);
    }

    private static DemoRequestSubmission ValidSubmission() =>
        new("School", "Contact", "contact@example.com", null, "PL", "Warsaw", 500, null, true);

    private static DemoRequest NewRequest(DemoRequestStatus status) =>
        new()
        {
            Id = Guid.NewGuid(),
            SchoolName = "School",
            ContactName = "Contact",
            WorkEmail = "contact@example.com",
            NormalizedWorkEmail = "CONTACT@EXAMPLE.COM",
            CountryCode = "PL",
            City = "Warsaw",
            EstimatedStudentCount = 500,
            Status = status,
            PrivacyConsentAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            RowVersion = BitConverter.GetBytes(1L)
        };

    private sealed class FakeRepository : ICustomerOnboardingRepository
    {
        public List<DemoRequest> Requests { get; } = [];
        public DemoAccess? Access { get; set; }
        public DateTime? LastDemoStart { get; set; }
        public DateTime? LastDemoExpiry { get; set; }

        public Task<bool> ExistsOpenByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
            Task.FromResult(Requests.Any(x => x.NormalizedWorkEmail == normalizedEmail && x.Status != DemoRequestStatus.Won && x.Status != DemoRequestStatus.Lost));

        public Task<IReadOnlyList<DemoRequest>> ListRequestsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DemoRequest>>(Requests.ToArray());

        public Task<DemoRequest?> GetRequestAsync(Guid requestId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Requests.SingleOrDefault(x => x.Id == requestId));

        public Task<DemoAccess?> GetDemoAccessByRequestAsync(Guid requestId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Access?.DemoRequestId == requestId ? Access : null);

        public Task<DemoAccess?> GetDemoAccessBySchoolAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Access?.SchoolId == schoolId ? Access : null);

        public Task<CustomerOnboardingWriteResult> AddRequestAsync(DemoRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            request.RowVersion = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
            return Task.FromResult(CustomerOnboardingWriteResult.Success());
        }

        public Task<CustomerOnboardingWriteResult> SaveRequestAsync(DemoRequest request, byte[] expectedRowVersion, CancellationToken cancellationToken = default)
        {
            request.RowVersion = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
            return Task.FromResult(CustomerOnboardingWriteResult.Success());
        }

        public Task<CustomerOnboardingWriteResult> SaveDemoAccessAsync(DemoAccess access, byte[] expectedRowVersion, CancellationToken cancellationToken = default)
        {
            Access = access;
            access.RowVersion = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
            return Task.FromResult(CustomerOnboardingWriteResult.Success());
        }

        public Task<CustomerOnboardingProvisionResult> CreateDemoAsync(
            Guid requestId,
            byte[] expectedRequestRowVersion,
            DateTime startsAtUtc,
            DateTime expiresAtUtc,
            CancellationToken cancellationToken = default)
        {
            LastDemoStart = startsAtUtc;
            LastDemoExpiry = expiresAtUtc;
            var schoolId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            Access = new DemoAccess
            {
                Id = Guid.NewGuid(),
                DemoRequestId = requestId,
                SchoolId = schoolId,
                SchoolAdminUserId = userId,
                StartsAtUtc = startsAtUtc,
                ExpiresAtUtc = expiresAtUtc,
                CreatedAtUtc = startsAtUtc,
                UpdatedAtUtc = startsAtUtc,
                RowVersion = BitConverter.GetBytes(1L)
            };
            return Task.FromResult(CustomerOnboardingProvisionResult.Success(
                schoolId, userId, "token", "contact@example.com", "Demo School", "en"));
        }

        public Task<CustomerOnboardingProvisionResult> ProvisionCustomerAsync(
            Guid requestId,
            byte[] expectedRequestRowVersion,
            string normalizedSchoolCode,
            string defaultCulture,
            string timeZoneId,
            DateTime utcNow,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CustomerOnboardingProvisionResult.Success(
                Guid.NewGuid(), Guid.NewGuid(), "token", "contact@example.com", "School", defaultCulture));
    }
}
