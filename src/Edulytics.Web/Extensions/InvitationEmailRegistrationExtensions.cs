using Edulytics.Web.Email;

namespace Edulytics.Web.Extensions;

public static class InvitationEmailRegistrationExtensions
{
    public static IServiceCollection
        AddInvitationEmailDelivery(
            this IServiceCollection services,
            IConfiguration configuration)
    {
        services.Configure<SmtpEmailOptions>(
            configuration.GetSection(
                SmtpEmailOptions.SectionName));

        services.AddHttpContextAccessor();

        services.AddSingleton<
            EmailConnectorCircuitBreaker>();

        services.AddScoped<
            IInvitationEmailTemplateRenderer,
            InvitationEmailTemplateRenderer>();

        services.AddScoped<
            MailKitUserInvitationDeliveryService>();

        services.AddScoped<
            IUserInvitationConnector>(
                provider =>
                    provider.GetRequiredService<
                        MailKitUserInvitationDeliveryService>());

        // Request path only queues durable delivery.
        services.AddScoped<
            IUserInvitationDeliveryService,
            DurableUserInvitationDeliveryService>();

        return services;
    }
}
