using Edulytics.Core.Entities;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Tests.Phase10;

public sealed class RealtimeModelTests
{
    [Fact]
    public void Outbox_IsMappedWithRowVersionAndUniqueCorrelation()
    {
        using var db = CreateDb();

        var entity =
            db.Model.FindEntityType(
                typeof(OutboxMessage));

        Assert.NotNull(entity);

        var rowVersion =
            entity!.FindProperty(
                nameof(OutboxMessage.RowVersion));

        Assert.NotNull(rowVersion);
        Assert.True(
            rowVersion!.IsConcurrencyToken);

        Assert.Contains(
            entity.GetIndexes(),
            index =>
                index.IsUnique &&
                index.Properties
                    .Select(x => x.Name)
                    .SequenceEqual(
                        [
                            nameof(
                                OutboxMessage.CorrelationId)
                        ]));

        Assert.Contains(
            entity.GetIndexes(),
            index =>
                index.Properties.Any(
                    x =>
                        x.Name ==
                        nameof(
                            OutboxMessage.ProcessedAtUtc)));
    }

    private static EdulyticsDbContext CreateDb()
    {
        var options =
            new DbContextOptionsBuilder<EdulyticsDbContext>()
                .UseInMemoryDatabase(
                    $"p10-model-{Guid.NewGuid():N}")
                .Options;

        return new EdulyticsDbContext(options);
    }
}
