using Edulytics.Web;
using System.Globalization;
using Edulytics.Web.Bootstrap;
using Edulytics.Web.Extensions;
using Edulytics.Web.Localization;
using Microsoft.AspNetCore.Localization;

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

app.UseHttpsRedirection();

app.UseRequestLocalization();

app.UseRouting();

app.UseAuthentication();
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

app.Run();

public partial class Program
{
}
