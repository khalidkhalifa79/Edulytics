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
        MathematicsCurriculumPackRegistry.CambridgeCode,
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

            await RetireObsoleteEnglandAsync(ct);

            await transaction.CommitAsync(ct);
            return;
        }

        foreach (var doc in docs.OrderBy(x => x.PackCode, StringComparer.Ordinal))
            await SeedOneAsync(doc, ct);

        await RetireObsoleteEnglandAsync(ct);
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

        var official =
            d.Nodes
                .Where(
                    x =>
                        x.IsOfficial &&
                        (
                            d.PackCode ==
                                MathematicsCurriculumPackRegistry.CambridgeCode
                                ? x.Kind is "Outcome" or "Reference"
                                : x.Kind is "Standard" or "Outcome"
                        ))
                .ToArray();
        if (official.Length != d.OfficialNodeCount)
            throw new InvalidOperationException($"Official count mismatch: {d.PackCode}");

        var full = d.TextMode == "FullOfficialTextPermitted";
        if (full && official.Any(x => string.IsNullOrWhiteSpace(x.OfficialText)))
            throw new InvalidOperationException($"Full-text pack missing official text: {d.PackCode}");
        if (!full && official.Any(x => !string.IsNullOrWhiteSpace(x.OfficialText)))
            throw new InvalidOperationException($"Source-linked pack leaked full official text: {d.PackCode}");

        if (d.PackCode ==
            MathematicsCurriculumPackRegistry.CambridgeCode)
        {
            ValidateCambridgeManifest(d);
        }
        if (d.PackCode == MathematicsCurriculumPackRegistry.CommonCoreCode &&
            (d.SchemaVersion != 14 ||
             d.OfficialNodeCount != 393 ||
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

    private static void ValidateCambridgeManifest(
        Doc d)
    {
        if (d.SchemaVersion != 16 ||
            d.VersionCode != "CAMBRIDGE-PATHWAY-2026" ||
            d.TextMode != "OfficialSourceLinked" ||
            d.ReuseBasis != "CopyrightedOfficialSourceReference" ||
            d.OfficialNodeCount != 779 ||
            d.NodeCount != 888 ||
            d.UnitCount != 0 ||
            d.LessonCount != 0 ||
            d.LinkCount != 0)
        {
            throw new InvalidOperationException(
                "Cambridge accepted baseline contract drift.");
        }

        if (d.Nodes.Any(
                x =>
                    !string.IsNullOrWhiteSpace(
                        x.OfficialText)))
        {
            throw new InvalidOperationException(
                "Cambridge copyrighted official prose must not be persisted.");
        }

        var assembly =
            typeof(MathematicsCurriculumPackRegistry)
                .Assembly;

        var names =
            assembly
                .GetManifestResourceNames()
                .Where(
                    x =>
                        x.EndsWith(
                            "cambridge-intl-math.integrity-manifest.json",
                            StringComparison.OrdinalIgnoreCase))
                .ToArray();

        if (names.Length != 1)
        {
            throw new InvalidOperationException(
                "Exactly one Cambridge integrity manifest is required.");
        }

        using var stream =
            assembly.GetManifestResourceStream(
                names[0])
            ?? throw new InvalidOperationException(
                "Cambridge integrity manifest cannot be opened.");

        using var document =
            JsonDocument.Parse(stream);

        var root =
            document.RootElement;

        var counts =
            root.GetProperty("Counts");

        var policy =
            root.GetProperty("Policy");

        if (root.GetProperty("PackCode").GetString() != d.PackCode ||
            root.GetProperty("VersionCode").GetString() != d.VersionCode ||
            root.GetProperty("SourceDigest").GetString() != d.SourceDigest ||
            root.GetProperty("ContentDigest").GetString() != d.ContentDigest ||
            counts.GetProperty("Primary0096").GetInt32() != 296 ||
            counts.GetProperty("Lower0862").GetInt32() != 187 ||
            counts.GetProperty("Igcse0580CoreSections").GetInt32() != 53 ||
            counts.GetProperty("Igcse0580CoreLeafReferences").GetInt32() != 100 ||
            counts.GetProperty("Igcse0580ExtendedSections").GetInt32() != 72 ||
            counts.GetProperty("Igcse0580ExtendedLeafReferences").GetInt32() != 158 ||
            counts.GetProperty("Alevel9709TopicReferences").GetInt32() != 38 ||
            counts.GetProperty("OfficialReferenceIdentifiers").GetInt32() != 779 ||
            counts.GetProperty("Nodes").GetInt32() != 888)
        {
            throw new InvalidOperationException(
                "Cambridge integrity manifest drift.");
        }

        if (policy.GetProperty("OfficialCambridgeProseStored").GetBoolean() ||
            policy.GetProperty("CurriculumTranslated").GetBoolean() ||
            policy.GetProperty("InventedIdentifiers").GetBoolean() ||
            policy.GetProperty("SyntheticLessons").GetBoolean() ||
            policy.GetProperty("OutcomeFallbackForCambridge").GetBoolean())
        {
            throw new InvalidOperationException(
                "Cambridge rights/product policy drift.");
        }

        var codes =
            d.Nodes
                .Select(x => x.Code)
                .ToHashSet(StringComparer.Ordinal);

        foreach (var required in new[]
        {
            "CAM:OUT:0580:C1.15.1",
            "CAM:OUT:0580:C1.15.2",
            "CAM:OUT:0580:C1.15.3",
            "CAM:OUT:0580:C4.1.1",
            "CAM:OUT:0580:C4.1.2",
            "CAM:OUT:0580:C4.1.3",
            "CAM:OUT:0580:C8.2.1",
            "CAM:OUT:0580:C8.2.2",
            "CAM:OUT:0580:E8.2.1",
            "CAM:OUT:0580:E8.2.2",
            "CAM:REF:9709:1.1",
            "CAM:REF:9709:6.5"
        })
        {
            if (!codes.Contains(required))
            {
                throw new InvalidOperationException(
                    $"Cambridge accepted reference missing: {required}.");
            }
        }
    }

    private async Task RetireObsoleteEnglandAsync(
        CancellationToken ct)
    {
        // Historical platform identity only.
        //
        // Existing school data is NOT automatically remapped to Cambridge.
        // The historical framework is retained for auditability but removed
        // from the active curriculum surface.
        const string obsoleteCode =
            "UK-NC-ENG-MATH";

        var normalized =
            obsoleteCode.ToUpperInvariant();

        var frameworks =
            await _db.CurriculumFrameworks
                .Where(
                    x =>
                        x.OwnerSchoolId == null &&
                        x.NormalizedCode == normalized)
                .ToArrayAsync(ct);

        if (frameworks.Length == 0)
        {
            return;
        }

        var now =
            DateTime.UtcNow;

        var frameworkIds =
            frameworks
                .Select(x => x.Id)
                .ToArray();

        var versions =
            await _db.CurriculumFrameworkVersions
                .Where(
                    x =>
                        frameworkIds.Contains(
                            x.FrameworkId))
                .ToArrayAsync(ct);

        var versionIds =
            versions
                .Select(x => x.Id)
                .ToArray();

        var nodes =
            await _db.CurriculumPackContentNodes
                .Where(
                    x =>
                        versionIds.Contains(
                            x.FrameworkVersionId))
                .ToArrayAsync(ct);

        foreach (var framework in frameworks)
        {
            framework.IsActive = false;
            framework.UpdatedAtUtc = now;
        }

        foreach (var version in versions)
        {
            version.IsActive = false;
            version.UpdatedAtUtc = now;
        }

        foreach (var node in nodes)
        {
            node.IsActive = false;
            node.UpdatedAtUtc = now;
        }

        await _db.SaveChangesAsync(ct);
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
            if (StateMatchesDocument(state, d))
            {
                await ValidatePersistedRowsAsync(
                    d,
                    version.Id,
                    ct);

                return;
            }

            if (await TryRepairAcceptedCommonCoreV14Async(
                    d,
                    state,
                    version.Id,
                    ct))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Immutable accepted pack drift: {d.PackCode}");
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

    private static bool StateMatchesDocument(
        CurriculumPackImportState state,
        Doc d)
    {
        return
            state.SourceDigest == d.SourceDigest &&
            state.ContentDigest == d.ContentDigest &&
            state.NodeCount == d.NodeCount &&
            state.OfficialNodeCount == d.OfficialNodeCount &&
            state.UnitCount == d.UnitCount &&
            state.LessonCount == d.LessonCount &&
            state.LinkCount == d.LinkCount &&
            state.IsComplete;
    }

    private async Task ValidatePersistedRowsAsync(
        Doc d,
        Guid frameworkVersionId,
        CancellationToken ct)
    {
        var persisted =
            await _db.CurriculumPackContentNodes
                .AsNoTracking()
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                        frameworkVersionId)
                .ToArrayAsync(ct);

        var linkCount =
            await _db.CurriculumPackNodeLinks
                .AsNoTracking()
                .CountAsync(
                    x =>
                        x.FrameworkVersionId ==
                        frameworkVersionId,
                    ct);

        if (persisted.Length != d.NodeCount ||
            linkCount != d.LinkCount)
        {
            throw new InvalidOperationException(
                $"Persisted rows drift: {d.PackCode}");
        }

        if (d.PackCode !=
                MathematicsCurriculumPackRegistry.CommonCoreCode &&
            d.PackCode !=
                MathematicsCurriculumPackRegistry.CambridgeCode)
        {
            return;
        }

        var officialCount =
            persisted.Count(
                x =>
                    x.IsOfficial &&
                    (
                        d.PackCode ==
                            MathematicsCurriculumPackRegistry.CambridgeCode
                            ? x.NodeKind is "Outcome" or "Reference"
                            : x.NodeKind is "Standard" or "Outcome"
                    ));

        if (officialCount != d.OfficialNodeCount)
        {
            throw new InvalidOperationException(
                $"Persisted official-node count drift: {d.PackCode}.");
        }

        var expectedIds =
            d.Nodes.ToDictionary(
                x => x.Code,
                x => G(
                    $"node|{d.PackCode}|{d.VersionCode}|{x.Code}"),
                StringComparer.Ordinal);

        var persistedByCode =
            persisted.ToDictionary(
                x => x.Code,
                StringComparer.Ordinal);

        if (persistedByCode.Count != d.Nodes.Count)
        {
            throw new InvalidOperationException(
                $"Persisted code-set cardinality drift: {d.PackCode}.");
        }

        foreach (var expected in d.Nodes)
        {
            if (!persistedByCode.TryGetValue(
                    expected.Code,
                    out var current))
            {
                throw new InvalidOperationException(
                    $"Persisted node missing for {d.PackCode}: {expected.Code}");
            }

            Guid? parentId =
                expected.ParentCode is null
                    ? null
                    : expectedIds[expected.ParentCode];

            if (!PersistedNodeMatchesDocument(
                    current,
                    d,
                    expected,
                    expectedIds[expected.Code],
                    parentId,
                    frameworkVersionId))
            {
                throw new InvalidOperationException(
                    $"Persisted node drift for {d.PackCode}: {expected.Code}");
            }
        }
    }

    private async Task<bool> TryRepairAcceptedCommonCoreV14Async(
        Doc d,
        CurriculumPackImportState state,
        Guid frameworkVersionId,
        CancellationToken ct)
    {
        if (d.PackCode !=
                MathematicsCurriculumPackRegistry.CommonCoreCode ||
            state.FrameworkCode !=
                MathematicsCurriculumPackRegistry.CommonCoreCode ||
            state.VersionCode !=
                "CCSSM-2010")
        {
            return false;
        }

        var manifest =
            LoadCommonCoreIntegrityManifest();

        var legacy =
            manifest.Legacy;

        var previousCorrected =
            manifest.PreviousCorrected;

        var legacyState =
            StateMatchesRepair(
                state,
                legacy);

        var previousCorrectedState =
            StateMatchesRepair(
                state,
                previousCorrected);

        if (!legacyState &&
            !previousCorrectedState)
        {
            return false;
        }

        if (legacyState &&
            previousCorrectedState)
        {
            throw new InvalidOperationException(
                "Ambiguous Common Core historical repair fingerprint.");
        }

        if (manifest.ManifestVersion != 2 ||
            manifest.PackCode != d.PackCode ||
            manifest.VersionCode != d.VersionCode ||
            d.SchemaVersion != 14 ||
            manifest.SourcePdfSha256 !=
                "1dc360aa21390c2860c939f731b693295ee1537cbb2b2e3be2ccd06dcb06898c" ||
            manifest.CorrectedContentDigest !=
                d.ContentDigest ||
            manifest.CorrectedNodeCount !=
                d.NodeCount ||
            manifest.CorrectedOfficialNodeCount !=
                d.OfficialNodeCount ||
            manifest.CorrectedNodeCount != 459 ||
            manifest.CorrectedOfficialNodeCount != 393 ||
            manifest.CorrectedDomainCount != 66 ||
            manifest.NumberedStandardCount != 385 ||
            manifest.K8NumberedStandardCount != 229 ||
            manifest.HighSchoolNumberedStandardCount != 156 ||
            manifest.TrailingContaminationRepairs != 140 ||
            manifest.NumberedStandardTextSha256.Count != 385 ||
            legacy.SourceDigest !=
                d.SourceDigest ||
            legacy.ContentDigest !=
                "e265387773923fd77eed959d49fa87e983171d917b894c2cff7c8ca2085526d5" ||
            legacy.NodeCount != 420 ||
            legacy.OfficialNodeCount != 360 ||
            legacy.UnitCount != 0 ||
            legacy.LessonCount != 0 ||
            legacy.LinkCount != 0 ||
            legacy.MissingNodeCodes.Count != 38 ||
            legacy.ChangedNodeContentHashes.Count != 140 ||
            previousCorrected.SourceDigest !=
                d.SourceDigest ||
            previousCorrected.ContentDigest !=
                "5cab24d5f9b3d27839207db19ca8182d69acca4c4e252e3d8da822cb64541e82" ||
            previousCorrected.NodeCount != 458 ||
            previousCorrected.OfficialNodeCount != 392 ||
            previousCorrected.UnitCount != 0 ||
            previousCorrected.LessonCount != 0 ||
            previousCorrected.LinkCount != 0 ||
            previousCorrected.MissingNodeCodes.Count != 1 ||
            previousCorrected.MissingNodeCodes[0] !=
                "CCSS:1.OA.B.3" ||
            previousCorrected.ChangedNodeContentHashes.Count != 439)
        {
            throw new InvalidOperationException(
                "Common Core V14 repair manifest contract drift.");
        }

        var repair =
            legacyState
                ? legacy
                : previousCorrected;

        var existing =
            await _db.CurriculumPackContentNodes
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                        frameworkVersionId)
                .ToArrayAsync(ct);

        var existingLinkCount =
            await _db.CurriculumPackNodeLinks
                .CountAsync(
                    x =>
                        x.FrameworkVersionId ==
                        frameworkVersionId,
                    ct);

        if (existing.Length !=
                repair.NodeCount ||
            existingLinkCount !=
                repair.LinkCount)
        {
            throw new InvalidOperationException(
                "Historical Common Core persisted-row fingerprint drift.");
        }

        var expectedIds =
            d.Nodes.ToDictionary(
                x => x.Code,
                x => G(
                    $"node|{d.PackCode}|{d.VersionCode}|{x.Code}"),
                StringComparer.Ordinal);

        var expectedByCode =
            d.Nodes.ToDictionary(
                x => x.Code,
                StringComparer.Ordinal);

        var existingByCode =
            existing.ToDictionary(
                x => x.Code,
                StringComparer.Ordinal);

        var actualMissing =
            expectedByCode.Keys
                .Except(
                    existingByCode.Keys,
                    StringComparer.Ordinal)
                .ToHashSet(
                    StringComparer.Ordinal);

        var expectedMissing =
            legacyState
                ? legacy.MissingNodeCodes
                    .Append(
                        "CCSS:1.OA.B.3")
                    .ToHashSet(
                        StringComparer.Ordinal)
                : previousCorrected
                    .MissingNodeCodes
                    .ToHashSet(
                        StringComparer.Ordinal);

        var stale =
            existingByCode.Keys
                .Except(
                    expectedByCode.Keys,
                    StringComparer.Ordinal)
                .ToArray();

        if (!expectedMissing.SetEquals(
                actualMissing) ||
            stale.Length != 0)
        {
            throw new InvalidOperationException(
                "Historical Common Core code-set fingerprint drift.");
        }

        var now =
            DateTime.UtcNow;

        foreach (var expected in
                 d.Nodes.OrderBy(x => x.SortOrder))
        {
            var expectedId =
                expectedIds[
                    expected.Code];

            Guid? parentId =
                expected.ParentCode is null
                    ? null
                    : expectedIds[
                        expected.ParentCode];

            if (existingByCode.TryGetValue(
                    expected.Code,
                    out var current))
            {
                if (!PersistedNodeStaticMetadataMatchesExceptSortOrder(
                        current,
                        d,
                        expected,
                        expectedId,
                        parentId,
                        frameworkVersionId))
                {
                    throw new InvalidOperationException(
                        $"Unexpected historical Common Core metadata drift: " +
                        $"{expected.Code}");
                }

                if (current.SortOrder ==
                        expected.SortOrder &&
                    current.OfficialText ==
                        expected.OfficialText &&
                    current.ContentHash ==
                        expected.ContentHash)
                {
                    continue;
                }

                string? acceptedHistoricalHash =
                    null;

                if (legacyState &&
                    legacy.ChangedNodeContentHashes
                        .TryGetValue(
                            expected.Code,
                            out var legacyHash))
                {
                    acceptedHistoricalHash =
                        legacyHash;
                }
                else if (
                    previousCorrected
                        .ChangedNodeContentHashes
                        .TryGetValue(
                            expected.Code,
                            out var previousHash))
                {
                    acceptedHistoricalHash =
                        previousHash;
                }

                if (acceptedHistoricalHash is null ||
                    current.ContentHash !=
                        acceptedHistoricalHash)
                {
                    throw new InvalidOperationException(
                        $"Unexpected historical Common Core content/order drift: " +
                        $"{expected.Code}");
                }

                current.OfficialText =
                    expected.OfficialText;

                current.SortOrder =
                    expected.SortOrder;

                current.ContentHash =
                    expected.ContentHash;

                current.UpdatedAtUtc =
                    now;

                continue;
            }

            _db.CurriculumPackContentNodes.Add(
                new CurriculumPackContentNode
                {
                    Id =
                        expectedId,

                    FrameworkVersionId =
                        frameworkVersionId,

                    FrameworkCode =
                        d.PackCode,

                    VersionCode =
                        d.VersionCode,

                    NodeKind =
                        expected.Kind,

                    Code =
                        expected.Code,

                    ParentId =
                        parentId,

                    LogicalLevelFrom =
                        expected.LogicalLevelFrom,

                    LogicalLevelTo =
                        expected.LogicalLevelTo,

                    NativeLevel =
                        expected.NativeLevel,

                    Pathway =
                        expected.Pathway,

                    Title =
                        expected.Title,

                    OfficialText =
                        expected.OfficialText,

                    AuthorDescription =
                        expected.AuthorDescription,

                    SourceAuthority =
                        expected.SourceAuthority,

                    SourceUrl =
                        expected.SourceUrl,

                    SourceLocator =
                        expected.SourceLocator,

                    Attribution =
                        expected.Attribution,

                    IsOfficial =
                        expected.IsOfficial,

                    IsActive =
                        expected.IsActive,

                    SortOrder =
                        expected.SortOrder,

                    ContentHash =
                        expected.ContentHash,

                    CreatedAtUtc =
                        now,

                    UpdatedAtUtc =
                        now
                });
        }

        state.SourceDigest =
            d.SourceDigest;

        state.ContentDigest =
            d.ContentDigest;

        state.NodeCount =
            d.NodeCount;

        state.OfficialNodeCount =
            d.OfficialNodeCount;

        state.UnitCount =
            d.UnitCount;

        state.LessonCount =
            d.LessonCount;

        state.LinkCount =
            d.LinkCount;

        state.IsComplete =
            true;

        state.ImportedAtUtc =
            now;

        await _db.SaveChangesAsync(ct);

        await ValidatePersistedRowsAsync(
            d,
            frameworkVersionId,
            ct);

        return true;
    }

    private static bool StateMatchesRepair(
        CurriculumPackImportState state,
        CommonCoreLegacyRepair repair)
    {
        return
            state.SourceDigest ==
                repair.SourceDigest &&
            state.ContentDigest ==
                repair.ContentDigest &&
            state.NodeCount ==
                repair.NodeCount &&
            state.OfficialNodeCount ==
                repair.OfficialNodeCount &&
            state.UnitCount ==
                repair.UnitCount &&
            state.LessonCount ==
                repair.LessonCount &&
            state.LinkCount ==
                repair.LinkCount &&
            state.IsComplete;
    }

    private static bool PersistedNodeStaticMetadataMatchesExceptSortOrder(
        CurriculumPackContentNode current,
        Doc d,
        Node expected,
        Guid expectedId,
        Guid? expectedParentId,
        Guid expectedFrameworkVersionId)
    {
        return
            current.Id ==
                expectedId &&

            current.FrameworkVersionId ==
                expectedFrameworkVersionId &&

            current.FrameworkCode ==
                d.PackCode &&

            current.VersionCode ==
                d.VersionCode &&

            current.NodeKind ==
                expected.Kind &&

            current.Code ==
                expected.Code &&

            current.ParentId ==
                expectedParentId &&

            current.LogicalLevelFrom ==
                expected.LogicalLevelFrom &&

            current.LogicalLevelTo ==
                expected.LogicalLevelTo &&

            current.NativeLevel ==
                expected.NativeLevel &&

            current.Pathway ==
                expected.Pathway &&

            current.Title ==
                expected.Title &&

            current.AuthorDescription ==
                expected.AuthorDescription &&

            current.SourceAuthority ==
                expected.SourceAuthority &&

            current.SourceUrl ==
                expected.SourceUrl &&

            current.SourceLocator ==
                expected.SourceLocator &&

            current.Attribution ==
                expected.Attribution &&

            current.IsOfficial ==
                expected.IsOfficial &&

            current.IsActive ==
                expected.IsActive;
    }

    private static bool PersistedNodeStaticMetadataMatches(
        CurriculumPackContentNode current,
        Doc d,
        Node expected,
        Guid expectedId,
        Guid? expectedParentId,
        Guid expectedFrameworkVersionId)
    {
        return
            PersistedNodeStaticMetadataMatchesExceptSortOrder(
                current,
                d,
                expected,
                expectedId,
                expectedParentId,
                expectedFrameworkVersionId) &&

            current.SortOrder ==
                expected.SortOrder;
    }

    private static bool PersistedNodeMatchesDocument(
        CurriculumPackContentNode current,
        Doc d,
        Node expected,
        Guid expectedId,
        Guid? expectedParentId,
        Guid expectedFrameworkVersionId)
    {
        return
            PersistedNodeStaticMetadataMatches(
                current,
                d,
                expected,
                expectedId,
                expectedParentId,
                expectedFrameworkVersionId) &&

            current.OfficialText ==
                expected.OfficialText &&

            current.ContentHash ==
                expected.ContentHash;
    }

    private static CommonCoreIntegrityManifest
        LoadCommonCoreIntegrityManifest()
    {
        var assembly =
            typeof(MathematicsCurriculumPackRegistry)
                .Assembly;

        var names =
            assembly.GetManifestResourceNames()
                .Where(
                    x =>
                        x.EndsWith(
                            "us-ccss-math.integrity-manifest.json",
                            StringComparison.OrdinalIgnoreCase))
                .ToArray();

        if (names.Length != 1)
        {
            throw new InvalidOperationException(
                "Exactly one embedded Common Core integrity manifest is required.");
        }

        using var stream =
            assembly.GetManifestResourceStream(
                names[0])
            ?? throw new InvalidOperationException(
                "Cannot open embedded Common Core integrity manifest.");

        return
            JsonSerializer.Deserialize<CommonCoreIntegrityManifest>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive =
                        true
                })
            ?? throw new InvalidOperationException(
                "Invalid embedded Common Core integrity manifest.");
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

    private sealed class CommonCoreIntegrityManifest
    {
        public int ManifestVersion { get; set; }
        public string PackCode { get; set; } = "";
        public string VersionCode { get; set; } = "";
        public string SourcePdfSha256 { get; set; } = "";
        public string CorrectedContentDigest { get; set; } = "";
        public int CorrectedNodeCount { get; set; }
        public int CorrectedOfficialNodeCount { get; set; }
        public int CorrectedDomainCount { get; set; }
        public int NumberedStandardCount { get; set; }
        public int K8NumberedStandardCount { get; set; }
        public int HighSchoolNumberedStandardCount { get; set; }
        public int TrailingContaminationRepairs { get; set; }
        public CommonCoreLegacyRepair Legacy { get; set; } = new();
        public CommonCoreLegacyRepair PreviousCorrected { get; set; } = new();
        public Dictionary<string, string> NumberedStandardTextSha256 { get; set; } = [];
    }

    private sealed class CommonCoreLegacyRepair
    {
        public string SourceDigest { get; set; } = "";
        public string ContentDigest { get; set; } = "";
        public int NodeCount { get; set; }
        public int OfficialNodeCount { get; set; }
        public int UnitCount { get; set; }
        public int LessonCount { get; set; }
        public int LinkCount { get; set; }
        public List<string> MissingNodeCodes { get; set; } = [];
        public Dictionary<string, string> ChangedNodeContentHashes { get; set; } = [];
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
