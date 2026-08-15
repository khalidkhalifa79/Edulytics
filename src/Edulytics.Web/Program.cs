using Edulytics.Web.Hubs;
using System.Threading.RateLimiting;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using Edulytics.Web;
using System.Globalization;
using Edulytics.Web.Bootstrap;
using Edulytics.Web.Extensions;
using Edulytics.Web.Localization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(
        optional: true);
}

builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});

builder.Services
    .AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider =
            (_, factory) =>
                factory.Create(
                    typeof(ValidationResource));
    });

builder.Services.AddEdulyticsIdentityAndData(
    builder.Configuration);

var supportedCultures =
    CultureCookie.SupportedCultures
        .Select(x => new CultureInfo(x))
        .ToArray();

builder.Services.Configure<RequestLocalizationOptions>(
    options =>
    {
        options.DefaultRequestCulture =
            new RequestCulture("en");

        options.SupportedCultures =
            supportedCultures;

        options.SupportedUICultures =
            supportedCultures;

        options.RequestCultureProviders.Clear();

        options.RequestCultureProviders.Add(
            new CookieRequestCultureProvider
            {
                CookieName = CultureCookie.Name
            });
    });

builder.Services.AddSchoolManagementPhase04();
builder.Services.AddSchoolUserManagementPhase05();
builder.Services.AddAcademicStructurePhase06();
builder.Services.AddCurriculumPhase07();
builder.Services.AddAssessmentsPhase08();
builder.Services.AddAnalyticsPhase09();
builder.Services.AddRealtimeDashboardsPhase10();
builder.Services.AddInvitationEmailDelivery(
    builder.Configuration);

builder.Services.AddRateLimiter(
    options =>
    {
        options.RejectionStatusCode =
            StatusCodes.Status429TooManyRequests;

        options.AddPolicy(
            "SchoolUserCreate",
            context =>
            {
                var actor =
                    context.User.FindFirst(
                        ClaimTypes.NameIdentifier)
                        ?.Value
                    ?? context.Connection
                        .RemoteIpAddress
                        ?.ToString()
                    ?? "anonymous";

                return RateLimitPartition
                    .GetFixedWindowLimiter(
                        actor,
                        _ =>
                            new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 20,
                                Window =
                                    TimeSpan.FromMinutes(10),
                                QueueLimit = 0,
                                QueueProcessingOrder =
                                    QueueProcessingOrder
                                        .OldestFirst,
                                AutoReplenishment = true
                            });
            });

        options.AddPolicy(
            "InvitationResend",
            context =>
            {
                var actor =
                    context.User.FindFirst(
                        ClaimTypes.NameIdentifier)
                        ?.Value
                    ?? context.Connection
                        .RemoteIpAddress
                        ?.ToString()
                    ?? "anonymous";

                var target =
                    context.Request.RouteValues["id"]
                        ?.ToString()
                    ?? "unknown";

                return RateLimitPartition
                    .GetFixedWindowLimiter(
                        $"{actor}:{target}",
                        _ =>
                            new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 3,
                                Window =
                                    TimeSpan.FromMinutes(10),
                                QueueLimit = 0,
                                QueueProcessingOrder =
                                    QueueProcessingOrder
                                        .OldestFirst,
                                AutoReplenishment = true
                            });
            });

        options.AddPolicy(
            "PasswordSetup",
            context =>
            {
                var ip =
                    context.Connection
                        .RemoteIpAddress
                        ?.ToString()
                    ?? "unknown";

                return RateLimitPartition
                    .GetFixedWindowLimiter(
                        ip,
                        _ =>
                            new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 10,
                                Window =
                                    TimeSpan.FromMinutes(15),
                                QueueLimit = 0,
                                QueueProcessingOrder =
                                    QueueProcessingOrder
                                        .OldestFirst,
                                AutoReplenishment = true
                            });
            });
    });

builder.Services.Configure<ForwardedHeadersOptions>(
    options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor |
            ForwardedHeaders.XForwardedProto |
            ForwardedHeaders.XForwardedHost;

        if (string.Equals(
                Environment.GetEnvironmentVariable("CODESPACES"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        }
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var bootstrapper =
        scope.ServiceProvider
            .GetRequiredService<
                EdulyticsDatabaseBootstrapper>();

    await bootstrapper.InitializeAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseForwardedHeaders();

app.UseHttpsRedirection();

app.UseRequestLocalization();

app.UseRouting();

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapStaticAssets()
    .Add(endpointBuilder =>
        endpointBuilder.Metadata.Add(
            new Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute()));

app.MapControllerRoute(
        name: "default",
        pattern:
            "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<AnalyticsHub>("/hubs/analytics");

app.Run();

public partial class Program
{
}
