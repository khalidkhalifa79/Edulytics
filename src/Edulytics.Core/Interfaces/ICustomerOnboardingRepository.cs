using Edulytics.Core.Entities;
using Edulytics.Core.Onboarding;

namespace Edulytics.Core.Interfaces;

public interface ICustomerOnboardingRepository
{
    Task<bool> ExistsOpenByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DemoRequest>> ListRequestsAsync(
        CancellationToken cancellationToken = default);

    Task<DemoRequest?> GetRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<DemoAccess?> GetDemoAccessByRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<DemoAccess?> GetDemoAccessBySchoolAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<CustomerOnboardingWriteResult> AddRequestAsync(
        DemoRequest request,
        CancellationToken cancellationToken = default);

    Task<CustomerOnboardingWriteResult> SaveRequestAsync(
        DemoRequest request,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default);

    Task<CustomerOnboardingWriteResult> SaveDemoAccessAsync(
        DemoAccess access,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default);

    Task<CustomerOnboardingProvisionResult> CreateDemoAsync(
        Guid requestId,
        byte[] expectedRequestRowVersion,
        DateTime startsAtUtc,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default);

    Task<CustomerOnboardingProvisionResult> ProvisionCustomerAsync(
        Guid requestId,
        byte[] expectedRequestRowVersion,
        string normalizedSchoolCode,
        string defaultCulture,
        string timeZoneId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}
