using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Edulytics.Data.Contexts;

public sealed class EdulyticsDbContextFactory
    : IDesignTimeDbContextFactory<EdulyticsDbContext>
{
    public EdulyticsDbContext CreateDbContext(
        string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "ConnectionStrings__DefaultConnection");

        if (string.IsNullOrWhiteSpace(
                connectionString))
        {
            connectionString =
                Environment.GetEnvironmentVariable(
                    "EDULYTICS_CONNECTION_STRING");
        }

        if (string.IsNullOrWhiteSpace(
                connectionString))
        {
            throw new InvalidOperationException(
                "A PostgreSQL connection string is required "
                + "for EF design-time operations. Set "
                + "ConnectionStrings__DefaultConnection or "
                + "EDULYTICS_CONNECTION_STRING.");
        }

        var options =
            new DbContextOptionsBuilder<
                EdulyticsDbContext>()
                .UseNpgsql(
                    connectionString)
                .Options;

        return new EdulyticsDbContext(
            options);
    }
}
