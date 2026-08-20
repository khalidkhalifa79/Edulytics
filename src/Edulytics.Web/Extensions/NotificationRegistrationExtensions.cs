using Edulytics.Core.Interfaces;
using Edulytics.Data.Repositories;
using Edulytics.Services.Notifications;
using Edulytics.Web.Notifications;

namespace Edulytics.Web.Extensions;

public static class NotificationRegistrationExtensions
{
    public static IServiceCollection
        AddNotificationsPhase21(
            this IServiceCollection services)
    {
        services.AddScoped<
            INotificationRepository,
            NotificationRepository>();

        services.AddScoped<
            INotificationService,
            NotificationService>();

        services.AddScoped<
            INotificationDeliveryProcessor,
            NotificationDeliveryProcessor>();

        return services;
    }
}
