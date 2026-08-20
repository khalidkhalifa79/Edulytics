using System.Reflection;
using Edulytics.Web.Controllers;
using Edulytics.Web.Email;
using Edulytics.Web.Production;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edulytics.Tests.Phase22;

public sealed class Phase22OperationalAdminTests
{
    [Fact]
    public void OperationsController_IsPlatformAdministrationOnly()
    {
        var authorize =
            typeof(OperationsController)
                .GetCustomAttributes<
                    AuthorizeAttribute>()
                .ToArray();

        Assert.Contains(
            authorize,
            x =>
                x.Policy ==
                "PlatformAdministration");
    }

    [Fact]
    public void Requeue_IsAntiforgeryProtected()
    {
        var method =
            typeof(OperationsController)
                .GetMethod(
                    nameof(
                        OperationsController
                            .Requeue));

        Assert.NotNull(method);

        Assert.NotNull(
            method!
                .GetCustomAttribute<
                    ValidateAntiForgeryTokenAttribute>());
    }

    [Fact]
    public void OperationalView_NeverRendersRawOutboxPayload()
    {
        var view =
            ReadSource(
                "src/Edulytics.Web/Views/Operations/Index.cshtml");

        Assert.DoesNotContain(
            "PayloadJson",
            view,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ImportProjection_ExcludesUploadedPayloadAndIdentifiers()
    {
        var source =
            ReadSource(
                "src/Edulytics.Data/Repositories/OperationsRepository.cs");

        var projection =
            source[
                source.IndexOf(
                    "GetImportFailuresAsync",
                    StringComparison.Ordinal)..];

        Assert.DoesNotContain(
            "x.RowsJson",
            projection,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "x.FileHash",
            projection,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "x.OriginalFileName",
            projection,
            StringComparison.Ordinal);
    }

    [Fact]
    public void SafeRequeue_ResetsDeadLetterDependentJobs()
    {
        var source =
            ReadSource(
                "src/Edulytics.Data/Repositories/OutboxRepository.cs");

        Assert.Contains(
            "PrepareDependentStateForRequeueAsync",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "\"OutboxDeadLettered\"",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "\"BackgroundDeliveryDeadLettered\"",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "NotificationDeliveryStatus.Pending",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "ReportExportJobStatus.Pending",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ConnectorCircuitSnapshot_ReportsDegradation()
    {
        var circuit =
            new EmailConnectorCircuitBreaker();

        var now =
            new DateTime(
                2026,
                8,
                20,
                12,
                0,
                0,
                DateTimeKind.Utc);

        circuit.RecordFailure(
            now,
            failureThreshold: 1,
            breakSeconds: 60);

        var failed =
            circuit.Snapshot();

        Assert.Equal(
            1,
            failed.ConsecutiveFailures);

        Assert.Equal(
            now.AddSeconds(60),
            failed.OpenUntilUtc);

        circuit.RecordSuccess();

        var recovered =
            circuit.Snapshot();

        Assert.Equal(
            0,
            recovered.ConsecutiveFailures);

        Assert.Null(
            recovered.OpenUntilUtc);
    }

    [Fact]
    public void WorkerSnapshot_TracksStartAndHeartbeat()
    {
        var worker =
            new OutboxWorkerHealthState();

        var start =
            new DateTime(
                2026,
                8,
                20,
                12,
                0,
                0,
                DateTimeKind.Utc);

        worker.MarkStarted(start);
        worker.RecordHeartbeat(
            start.AddSeconds(5));

        var snapshot =
            worker.Snapshot();

        Assert.True(snapshot.Started);
        Assert.Equal(
            start,
            snapshot.StartedAtUtc);

        Assert.Equal(
            start.AddSeconds(5),
            snapshot.LastHeartbeatUtc);
    }

    [Fact]
    public void Console_ExposesRequiredOperationalSignals()
    {
        var service =
            ReadSource(
                "src/Edulytics.Web/Operations/OperationalConsoleService.cs");

        Assert.Contains(
            "ReleaseSha",
            service,
            StringComparison.Ordinal);

        Assert.Contains(
            "GetLatestMigrationAsync",
            service,
            StringComparison.Ordinal);

        Assert.Contains(
            "_worker.Snapshot",
            service,
            StringComparison.Ordinal);

        Assert.Contains(
            "_emailCircuit.Snapshot",
            service,
            StringComparison.Ordinal);
    }

    [Fact]
    public void PlatformDashboard_LinksToOperations()
    {
        var dashboard =
            ReadSource(
                "src/Edulytics.Web/Views/Platform/Dashboard.cshtml");

        Assert.Contains(
            "asp-controller=\"Operations\"",
            dashboard,
            StringComparison.Ordinal);

        Assert.Contains(
            "OpenOperations",
            dashboard,
            StringComparison.Ordinal);
    }

    private static string ReadSource(
        string relative)
    {
        var root =
            FindRepositoryRoot();

        return File.ReadAllText(
            Path.Combine(
                root,
                relative));
    }

    private static string FindRepositoryRoot()
    {
        var current =
            new DirectoryInfo(
                AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(
                    Path.Combine(
                        current.FullName,
                        "Edulytics.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Repository root was not found.");
    }
}
