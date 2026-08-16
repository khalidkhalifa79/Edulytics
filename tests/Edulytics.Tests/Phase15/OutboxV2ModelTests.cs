using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Tests.Phase15;

public sealed class OutboxV2ModelTests
{
    [Fact]
    public void Outbox_HasLeaseStatusAndDeadLetterIndexes()
    {
        using var db =
            CreateDb();

        var entity =
            db.Model.FindEntityType(
                typeof(OutboxMessage));

        Assert.NotNull(entity);

        Assert.NotNull(
            entity!.FindProperty(
                nameof(
                    OutboxMessage.Status)));

        Assert.NotNull(
            entity.FindProperty(
                nameof(
                    OutboxMessage.LeaseOwner)));

        Assert.NotNull(
            entity.FindProperty(
                nameof(
                    OutboxMessage.LeaseToken)));

        Assert.NotNull(
            entity.FindProperty(
                nameof(
                    OutboxMessage.LeaseUntilUtc)));

        Assert.True(
            entity.FindProperty(
                    nameof(
                        OutboxMessage.RowVersion))!
                .IsConcurrencyToken);

        Assert.Contains(
            entity.GetIndexes(),
            index =>
                index.Properties.Any(
                    x =>
                        x.Name ==
                        nameof(
                            OutboxMessage.Status)));
    }

    [Fact]
    public void AnalyticsRefreshState_IsOneRowPerSchool()
    {
        using var db =
            CreateDb();

        var entity =
            db.Model.FindEntityType(
                typeof(
                    AnalyticsRefreshState));

        Assert.NotNull(entity);

        Assert.Equal(
            nameof(
                AnalyticsRefreshState
                    .SchoolId),
            Assert.Single(
                    entity!
                        .FindPrimaryKey()!
                        .Properties)
                .Name);

        Assert.True(
            entity.FindProperty(
                    nameof(
                        AnalyticsRefreshState
                            .RowVersion))!
                .IsConcurrencyToken);
    }

    [Fact]
    public void Outbox_DefaultStatusIsPending()
    {
        var message =
            new OutboxMessage();

        Assert.Equal(
            OutboxMessageStatus.Pending,
            message.Status);
    }

    private static EdulyticsDbContext CreateDb()
    {
        var options =
            new DbContextOptionsBuilder<
                    EdulyticsDbContext>()
                .UseInMemoryDatabase(
                    $"p15-model-"
                    + $"{Guid.NewGuid():N}")
                .Options;

        return new EdulyticsDbContext(
            options);
    }
}
