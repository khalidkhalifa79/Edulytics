using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Edulytics.Core.Curriculum;
using Edulytics.Core.Entities;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Seeding;

public sealed class MathematicsCurriculumPackSeeder
{
    private static readonly HashSet<string> Expected =
    [
        MathematicsCurriculumPackRegistry.EnglandCode,
        MathematicsCurriculumPackRegistry.CommonCoreCode,
        MathematicsCurriculumPackRegistry.UaeCode,
        MathematicsCurriculumPackRegistry.PolandCode
    ];

    private readonly EdulyticsDbContext _db;
    public MathematicsCurriculumPackSeeder(EdulyticsDbContext db) => _db = db;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var docs = Load();
        if (docs.Count != 4 || !Expected.SetEquals(docs.Select(x => x.PackCode)))
            throw new InvalidOperationException("Exactly four approved embedded Mathematics packs are required.");

        foreach (var doc in docs)
            Validate(doc);

        if (_db.Database.IsNpgsql())
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(ct);
            await _db.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock(27500013);",
                ct);

            foreach (var doc in docs.OrderBy(x => x.PackCode, StringComparer.Ordinal))
                await SeedOneAsync(doc, ct);

            await transaction.CommitAsync(ct);
            return;
        }

        foreach (var doc in docs.OrderBy(x => x.PackCode, StringComparer.Ordinal))
            await SeedOneAsync(doc, ct);
    }

    private static List<Doc> Load()
    {
        var assembly = typeof(MathematicsCurriculumPackRegistry).Assembly;
        var names = assembly.GetManifestResourceNames()
            .Where(x => x.EndsWith(".curriculum-pack.json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        var result = new List<Doc>();
        foreach (var name in names)
        {
            using var stream = assembly.GetManifestResourceStream(name)
                ?? throw new InvalidOperationException($"Cannot open embedded resource {name}.");
            result.Add(JsonSerializer.Deserialize<Doc>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException($"Invalid embedded pack {name}."));
        }
        return result;
    }

    private static void Validate(Doc d)
    {
        if (!Expected.Contains(d.PackCode) || d.SubjectCode != "MATH" || d.Nodes.Count == 0)
            throw new InvalidOperationException($"Base pack contract failed: {d.PackCode}");

        if (d.NodeCount != d.Nodes.Count || d.LinkCount != d.Links.Count)
            throw new InvalidOperationException($"Persisted count contract failed: {d.PackCode}");

        var codes = d.Nodes.Select(x => x.Code).ToArray();
        if (codes.Length != codes.Distinct(StringComparer.Ordinal).Count())
            throw new InvalidOperationException($"Duplicate node codes: {d.PackCode}");
        var known = codes.ToHashSet(StringComparer.Ordinal);
        if (d.Nodes.Any(x => x.ParentCode is not null && !known.Contains(x.ParentCode)))
            throw new InvalidOperationException($"Dangling node parent: {d.PackCode}");
        if (d.Links.Any(x => !known.Contains(x.FromCode) || !known.Contains(x.ToCode)))
            throw new InvalidOperationException($"Dangling alignment link: {d.PackCode}");

        var official = d.Nodes.Where(x => x.IsOfficial && (x.Kind == "Standard" || x.Kind == "Outcome")).ToArray();
        if (official.Length != d.OfficialNodeCount)
            throw new InvalidOperationException($"Official count mismatch: {d.PackCode}");

        var full = d.TextMode == "FullOfficialTextPermitted";
        if (full && official.Any(x => string.IsNullOrWhiteSpace(x.OfficialText)))
            throw new InvalidOperationException($"Full-text pack missing official text: {d.PackCode}");
        if (!full && official.Any(x => !string.IsNullOrWhiteSpace(x.OfficialText)))
            throw new InvalidOperationException($"Source-linked pack leaked full official text: {d.PackCode}");

        if (d.PackCode == MathematicsCurriculumPackRegistry.EnglandCode && d.OfficialNodeCount != 436)
            throw new InvalidOperationException("England verified count must be 436.");
        if (d.PackCode == MathematicsCurriculumPackRegistry.CommonCoreCode &&
            (d.OfficialNodeCount != 360 ||
             d.ReuseBasis != "ProductOwnerConfirmedCommercialUseEvidence" ||
             !d.Attribution.Contains("Copyright 2010", StringComparison.Ordinal)))
            throw new InvalidOperationException("Common Core verified contract failed.");
        if (d.PackCode == MathematicsCurriculumPackRegistry.PolandCode && d.OfficialNodeCount != 306)
            throw new InvalidOperationException("Poland verified count must be 306.");

        if (d.PackCode == MathematicsCurriculumPackRegistry.UaeCode)
        {
            if (d.VersionCode != "MOE-2026-2027-T1" || d.OfficialNodeCount != 22 ||
                d.UnitCount != 6 || d.LessonCount != 42 || d.LinkCount != 48)
                throw new InvalidOperationException("UAE verified Grade 9 Advanced Term 1 contract failed.");
            if (d.Nodes.Any(x => x.LogicalLevelTo > 12) || d.Nodes.Any(x => x.Code.StartsWith("EDU:", StringComparison.Ordinal)))
                throw new InvalidOperationException("UAE grade/synthetic-node guard failed.");
            var lessons = d.Nodes.Where(x => x.Kind == "Lesson").Select(x => x.Code).ToHashSet(StringComparer.Ordinal);
            var linked = d.Links.Where(x => x.LinkKind == "LessonStandardAlignment").Select(x => x.FromCode).ToHashSet(StringComparer.Ordinal);
            if (!lessons.SetEquals(linked))
                throw new InvalidOperationException("Every verified UAE lesson must have at least one standard link.");
            if (d.Links.Any(x => !x.ToCode.StartsWith("UAE:STD:MAT.", StringComparison.Ordinal)))
                throw new InvalidOperationException("UAE lesson link points to a non-MAT standard.");
        }
        else if (d.UnitCount != 0 || d.LessonCount != 0 || d.LinkCount != 0)
        {
            throw new InvalidOperationException($"Only verified real lessons may be persisted; synthetic teaching shells are forbidden: {d.PackCode}");
        }
    }

    private async Task SeedOneAsync(Doc d, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var normalizedFramework = d.PackCode.ToUpperInvariant();

        var framework = await _db.CurriculumFrameworks.SingleOrDefaultAsync(
            x => x.OwnerSchoolId == null && x.NormalizedCode == normalizedFramework, ct);

        if (framework is null)
        {
            framework = new CurriculumFramework
            {
                Id = G($"framework|{d.PackCode}"),
                OwnerSchoolId = null,
                Code = d.PackCode,
                NormalizedCode = normalizedFramework,
                Name = d.DisplayName,
                CountryCode = d.CountryCode.Length >= 2 ? d.CountryCode[..2] : d.CountryCode,
                ProviderName = d.ProviderName,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            _db.CurriculumFrameworks.Add(framework);
            await _db.SaveChangesAsync(ct);
        }

        var normalizedVersion = d.VersionCode.ToUpperInvariant();
        var version = await _db.CurriculumFrameworkVersions.SingleOrDefaultAsync(
            x => x.FrameworkId == framework.Id && x.NormalizedVersionCode == normalizedVersion, ct);

        if (version is null)
        {
            version = new CurriculumFrameworkVersion
            {
                Id = G($"version|{d.PackCode}|{d.VersionCode}"),
                FrameworkId = framework.Id,
                VersionCode = d.VersionCode,
                NormalizedVersionCode = normalizedVersion,
                Name = d.VersionName,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            _db.CurriculumFrameworkVersions.Add(version);
            await _db.SaveChangesAsync(ct);
        }

        var state = await _db.CurriculumPackImportStates.SingleOrDefaultAsync(
            x => x.FrameworkVersionId == version.Id, ct);

        if (state is not null)
        {
            if (state.SourceDigest != d.SourceDigest || state.ContentDigest != d.ContentDigest ||
                state.NodeCount != d.NodeCount || state.OfficialNodeCount != d.OfficialNodeCount ||
                state.UnitCount != d.UnitCount || state.LessonCount != d.LessonCount ||
                state.LinkCount != d.LinkCount || !state.IsComplete)
                throw new InvalidOperationException($"Immutable accepted pack drift: {d.PackCode}");

            var nodeCount = await _db.CurriculumPackContentNodes.CountAsync(x => x.FrameworkVersionId == version.Id, ct);
            var linkCount = await _db.CurriculumPackNodeLinks.CountAsync(x => x.FrameworkVersionId == version.Id, ct);
            if (nodeCount != d.NodeCount || linkCount != d.LinkCount)
                throw new InvalidOperationException($"Persisted rows drift: {d.PackCode}");
            return;
        }

        if (await _db.CurriculumPackContentNodes.AnyAsync(x => x.FrameworkVersionId == version.Id, ct) ||
            await _db.CurriculumPackNodeLinks.AnyAsync(x => x.FrameworkVersionId == version.Id, ct))
            throw new InvalidOperationException($"Partial pack exists without import state: {d.PackCode}");

        var ids = d.Nodes.ToDictionary(
            x => x.Code,
            x => G($"node|{d.PackCode}|{d.VersionCode}|{x.Code}"),
            StringComparer.Ordinal);

        foreach (var x in d.Nodes.OrderBy(x => x.SortOrder))
        {
            _db.CurriculumPackContentNodes.Add(new CurriculumPackContentNode
            {
                Id = ids[x.Code],
                FrameworkVersionId = version.Id,
                FrameworkCode = d.PackCode,
                VersionCode = d.VersionCode,
                NodeKind = x.Kind,
                Code = x.Code,
                ParentId = x.ParentCode is null ? null : ids[x.ParentCode],
                LogicalLevelFrom = x.LogicalLevelFrom,
                LogicalLevelTo = x.LogicalLevelTo,
                NativeLevel = x.NativeLevel,
                Pathway = x.Pathway,
                Title = x.Title,
                OfficialText = x.OfficialText,
                AuthorDescription = x.AuthorDescription,
                SourceAuthority = x.SourceAuthority,
                SourceUrl = x.SourceUrl,
                SourceLocator = x.SourceLocator,
                Attribution = x.Attribution,
                IsOfficial = x.IsOfficial,
                IsActive = x.IsActive,
                SortOrder = x.SortOrder,
                ContentHash = x.ContentHash,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        foreach (var x in d.Links.OrderBy(x => x.SortOrder))
        {
            _db.CurriculumPackNodeLinks.Add(new CurriculumPackNodeLink
            {
                Id = G($"link|{d.PackCode}|{d.VersionCode}|{x.FromCode}|{x.ToCode}|{x.LinkKind}"),
                FrameworkVersionId = version.Id,
                FromNodeId = ids[x.FromCode],
                ToNodeId = ids[x.ToCode],
                LinkKind = x.LinkKind,
                AlignmentConfidence = x.Confidence,
                EvidenceNote = x.EvidenceNote,
                SortOrder = x.SortOrder,
                ContentHash = x.ContentHash,
                CreatedAtUtc = now
            });
        }

        _db.CurriculumPackImportStates.Add(new CurriculumPackImportState
        {
            Id = G($"state|{d.PackCode}|{d.VersionCode}"),
            FrameworkVersionId = version.Id,
            FrameworkCode = d.PackCode,
            VersionCode = d.VersionCode,
            SourceDigest = d.SourceDigest,
            ContentDigest = d.ContentDigest,
            NodeCount = d.NodeCount,
            OfficialNodeCount = d.OfficialNodeCount,
            UnitCount = d.UnitCount,
            LessonCount = d.LessonCount,
            LinkCount = d.LinkCount,
            IsComplete = true,
            ImportedAtUtc = now
        });

        await _db.SaveChangesAsync(ct);
    }

    private static Guid G(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        Span<byte> bytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(bytes);
        bytes[6] = (byte)((bytes[6] & 0x0f) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3f) | 0x80);
        return new Guid(bytes);
    }

    private sealed class Doc
    {
        public int SchemaVersion { get; set; }
        public string PackCode { get; set; } = "";
        public string VersionCode { get; set; } = "";
        public string VersionName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string CountryCode { get; set; } = "";
        public string ProviderName { get; set; } = "";
        public string SubjectCode { get; set; } = "";
        public string TextMode { get; set; } = "";
        public string ReuseBasis { get; set; } = "";
        public string Attribution { get; set; } = "";
        public string SourceDigest { get; set; } = "";
        public string ContentDigest { get; set; } = "";
        public int NodeCount { get; set; }
        public int OfficialNodeCount { get; set; }
        public int UnitCount { get; set; }
        public int LessonCount { get; set; }
        public int LinkCount { get; set; }
        public List<Node> Nodes { get; set; } = [];
        public List<Link> Links { get; set; } = [];
    }

    private sealed class Node
    {
        public string Code { get; set; } = "";
        public string Kind { get; set; } = "";
        public string? ParentCode { get; set; }
        public int LogicalLevelFrom { get; set; }
        public int LogicalLevelTo { get; set; }
        public string NativeLevel { get; set; } = "";
        public string? Pathway { get; set; }
        public string Title { get; set; } = "";
        public string? OfficialText { get; set; }
        public string? AuthorDescription { get; set; }
        public string SourceAuthority { get; set; } = "";
        public string SourceUrl { get; set; } = "";
        public string SourceLocator { get; set; } = "";
        public string Attribution { get; set; } = "";
        public bool IsOfficial { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public string ContentHash { get; set; } = "";
    }

    private sealed class Link
    {
        public string FromCode { get; set; } = "";
        public string ToCode { get; set; } = "";
        public string LinkKind { get; set; } = "";
        public string Confidence { get; set; } = "";
        public string EvidenceNote { get; set; } = "";
        public int SortOrder { get; set; }
        public string ContentHash { get; set; } = "";
    }
}
