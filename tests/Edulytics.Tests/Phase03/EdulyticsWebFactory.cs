using Edulytics.Data.Contexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Edulytics.Tests.Phase03;

public sealed class EdulyticsWebFactory
    : WebApplicationFactory<Program>
{
    private readonly string _databaseName =
        $"EdulyticsPhase03Tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(
            (_, configuration) =>
            {
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] =
                            "Server=localhost;Database=IgnoredForTests;"
                    });
            });

        builder.ConfigureServices(services =>
        {
            var descriptors = services
                .Where(descriptor =>
                    descriptor.ServiceType ==
                        typeof(
                            DbContextOptions<
                                EdulyticsDbContext>) ||
                    descriptor.ServiceType.Name.Contains(
                        "IDbContextOptionsConfiguration",
                        StringComparison.Ordinal))
                .ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<EdulyticsDbContext>(
                options =>
                    options.UseInMemoryDatabase(
                        _databaseName));
        });
    }
}
