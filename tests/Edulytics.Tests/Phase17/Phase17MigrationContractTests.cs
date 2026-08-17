using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Edulytics.Tests.Phase17;

public sealed class Phase17MigrationContractTests
{
    [Fact]
    public void
        Phase17MigrationTargetModelContainsPersistentDataProtectionStore()
    {
        var assembly =
            typeof(EdulyticsDbContext)
                .Assembly;

        var migrationType =
            assembly
                .GetTypes()
                .Single(
                    type =>
                        typeof(Migration)
                            .IsAssignableFrom(
                                type)
                        && type.Name
                            == "Phase17PersistDataProtectionKeys");

        var instance =
            Activator.CreateInstance(
                migrationType);

        var migration =
            Assert.IsAssignableFrom<
                Migration>(
                instance);

        AssertDataProtectionStore(
            migration.TargetModel);
    }

    [Fact]
    public void
        CurrentMigrationSnapshotContainsPersistentDataProtectionStore()
    {
        var assembly =
            typeof(EdulyticsDbContext)
                .Assembly;

        var snapshotType =
            assembly
                .GetTypes()
                .Single(
                    type =>
                        typeof(ModelSnapshot)
                            .IsAssignableFrom(
                                type)
                        && type.Name
                            == "EdulyticsDbContextModelSnapshot");

        var instance =
            Activator.CreateInstance(
                snapshotType,
                nonPublic:
                    true);

        var snapshot =
            Assert.IsAssignableFrom<
                ModelSnapshot>(
                instance);

        AssertDataProtectionStore(
            snapshot.Model);
    }

    private static void
        AssertDataProtectionStore(
            IModel model)
    {
        var entity =
            model
                .GetEntityTypes()
                .SingleOrDefault(
                    candidate =>
                        candidate
                            .GetTableName()
                        == "DataProtectionKeys");

        Assert.NotNull(
            entity);

        Assert.NotNull(
            entity!
                .FindProperty(
                    "Id"));

        Assert.NotNull(
            entity
                .FindProperty(
                    "FriendlyName"));

        Assert.NotNull(
            entity
                .FindProperty(
                    "Xml"));
    }
}
