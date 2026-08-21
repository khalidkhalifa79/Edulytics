using Edulytics.Core.Entities;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Tests.Phase25B;

public sealed class Phase25BModelTests
{
    [Fact]
    public void OnboardingEntities_AreMappedWithConcurrency()
    {
        var options = new DbContextOptionsBuilder<EdulyticsDbContext>()
            .UseInMemoryDatabase($"p25b-model-{Guid.NewGuid():N}")
            .Options;

        using var db = new EdulyticsDbContext(options);
        var request = db.Model.FindEntityType(typeof(DemoRequest));
        var access = db.Model.FindEntityType(typeof(DemoAccess));

        Assert.NotNull(request);
        Assert.NotNull(access);
        Assert.True(request!.FindProperty(nameof(DemoRequest.RowVersion))!.IsConcurrencyToken);
        Assert.True(access!.FindProperty(nameof(DemoAccess.RowVersion))!.IsConcurrencyToken);
        Assert.Contains(request.GetIndexes(), x => x.Properties.Any(p => p.Name == nameof(DemoRequest.NormalizedWorkEmail)));
        Assert.Contains(access.GetIndexes(), x => x.IsUnique && x.Properties.Single().Name == nameof(DemoAccess.DemoRequestId));
    }
}
