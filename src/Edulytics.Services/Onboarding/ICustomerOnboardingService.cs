using Edulytics.Core.Enums;

namespace Edulytics.Services.Onboarding;

public interface ICustomerOnboardingService
{
    Task<OnboardingCommandResult> SubmitDemoRequestAsync(
        DemoRequestSubmission request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DemoRequestListItem>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<DemoRequestDetails?> GetAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<OnboardingCommandResult> UpdateLeadAsync(
        Guid requestId,
        DemoRequestStatus targetStatus,
        DateTime? demoScheduledAtUtc,
        string? internalNote,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default);

    Task<OnboardingCommandResult> GrantDemoAsync(
        Guid requestId,
        byte[] expectedRequestRowVersion,
        CancellationToken cancellationToken = default);

    Task<OnboardingCommandResult> ExtendDemoAsync(
        Guid requestId,
        byte[] expectedAccessRowVersion,
        CancellationToken cancellationToken = default);

    Task<OnboardingCommandResult> ExpireDemoAsync(
        Guid requestId,
        byte[] expectedAccessRowVersion,
        CancellationToken cancellationToken = default);

    Task<OnboardingCommandResult> RevokeDemoAsync(
        Guid requestId,
        string reason,
        byte[] expectedAccessRowVersion,
        CancellationToken cancellationToken = default);

    Task<OnboardingCommandResult> ProvisionCustomerAsync(
        Guid requestId,
        string schoolCode,
        string defaultCulture,
        string timeZoneId,
        byte[] expectedRequestRowVersion,
        CancellationToken cancellationToken = default);
}
