using Edulytics.Core.Entities;
using Edulytics.Core.Interfaces;

namespace Edulytics.Services.Auditing;

public sealed class AuditService
    : IAuditService
{
    private readonly IAuditRepository _repository;
    private readonly IAuditRequestMetadataProvider
        _metadata;

    public AuditService(
        IAuditRepository repository,
        IAuditRequestMetadataProvider metadata)
    {
        _repository = repository;
        _metadata = metadata;
    }

    public Task QueueAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        _repository.Add(
            Build(
                auditEvent));

        return Task.CompletedTask;
    }

    public async Task RecordAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        _repository.Add(
            Build(
                auditEvent));

        await _repository
            .SaveChangesAsync(
                cancellationToken);
    }

    private AuditLog Build(
        AuditEvent auditEvent)
    {
        ArgumentNullException.ThrowIfNull(
            auditEvent);

        if (string.IsNullOrWhiteSpace(
                auditEvent.Action))
        {
            throw new ArgumentException(
                "Audit action is required.",
                nameof(auditEvent));
        }

        if (string.IsNullOrWhiteSpace(
                auditEvent.EntityType))
        {
            throw new ArgumentException(
                "Audit entity type is required.",
                nameof(auditEvent));
        }

        if (string.IsNullOrWhiteSpace(
                auditEvent.Feature))
        {
            throw new ArgumentException(
                "Audit feature is required.",
                nameof(auditEvent));
        }

        var metadata =
            _metadata.GetCurrent();

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            SchoolId =
                auditEvent.SchoolId,
            ActorUserId =
                auditEvent.ActorUserIdOverride
                ?? metadata.ActorUserId,
            ActorRole =
                Clamp(
                    auditEvent.ActorRoleOverride
                    ?? metadata.ActorRole,
                    100),
            Action =
                Clamp(
                    auditEvent.Action,
                    150),
            EntityType =
                Clamp(
                    auditEvent.EntityType,
                    150),
            EntityId =
                Clamp(
                    auditEvent.EntityId
                    ?? string.Empty,
                    100),
            OccurredAtUtc =
                DateTime.UtcNow,
            CorrelationId =
                Clamp(
                    auditEvent.CorrelationIdOverride
                    ?? metadata.CorrelationId,
                    100),
            IpAddress =
                Clamp(
                    metadata.IpAddress,
                    64),
            UserAgent =
                Clamp(
                    metadata.UserAgent,
                    512),
            OldValuesJson =
                AuditValueSanitizer.Serialize(
                    auditEvent.OldValues),
            NewValuesJson =
                AuditValueSanitizer.Serialize(
                    auditEvent.NewValues),
            ResultSummary =
                Clamp(
                    auditEvent.ResultSummary
                    ?? string.Empty,
                    500),
            Source =
                Clamp(
                    auditEvent.SourceOverride
                    ?? metadata.Source,
                    100),
            Feature =
                Clamp(
                    auditEvent.Feature,
                    100)
        };
    }

    private static string Clamp(
        string? value,
        int maxLength)
    {
        value =
            value?.Trim()
            ?? string.Empty;

        return value.Length <= maxLength
            ? value
            : value[..maxLength];
    }
}
