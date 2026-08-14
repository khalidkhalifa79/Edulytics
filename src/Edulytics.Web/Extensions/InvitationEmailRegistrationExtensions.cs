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

        services.AddScoped<
            IInvitationEmailTemplateRenderer,
            InvitationEmailTemplateRenderer>();

        services.AddScoped<
            IUserInvitationDeliveryService,
            MailKitUserInvitationDeliveryService>();

        return services;
    }
}
