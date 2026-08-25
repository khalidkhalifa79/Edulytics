using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Services.Schools;

namespace Edulytics.Tests.Phase04;

public sealed class SchoolManagementServiceTests
{
    [Fact]
    public void NormalizeSchoolCode_TrimsAndUppercases()
    {
        var result =
            SchoolManagementService.NormalizeSchoolCode("  waw-001 ");

        Assert.Equal("WAW-001", result);
    }

    [Fact]
    public async Task CreateAsync_RejectsInvalidSchoolCode()
    {
        var repository = new FakeSchoolRepository();
        var service = new SchoolManagementService(repository);

        var result = await service.CreateAsync(
            ValidCreate() with { SchoolCode = "WAW 001" });

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Errors,
            error => error.Code == SchoolErrorCode.InvalidSchoolCode);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateNormalizedSchoolCode()
    {
        var repository = new FakeSchoolRepository();

        repository.Seed(
            NewSchool(
                "Existing",
                "WAW-001",
                SchoolStatus.Active));

        var service = new SchoolManagementService(repository);

        var result = await service.CreateAsync(
            ValidCreate() with { SchoolCode = " waw-001 " });

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Errors,
            error => error.Code == SchoolErrorCode.DuplicateSchoolCode);
    }

    [Fact]
    public async Task CreateAsync_CreatesActiveSchool()
    {
        var repository = new FakeSchoolRepository();
        var service = new SchoolManagementService(repository);

        var result = await service.CreateAsync(ValidCreate());

        Assert.True(result.Succeeded);

        var school = Assert.Single(repository.Schools);

        Assert.Equal(SchoolStatus.Active, school.Status);
        Assert.Equal("WAW-001", school.SchoolCode);
        Assert.Equal("WAW-001", school.NormalizedSchoolCode);
        Assert.Equal("PL", school.CountryCode);
        Assert.Equal("Europe/Warsaw", school.TimeZoneId);
    }

    [Fact]
    public async Task CreateAsync_Uae_DerivesDubaiTimeZone()
    {
        var repository = new FakeSchoolRepository();
        var service = new SchoolManagementService(repository);

        var result = await service.CreateAsync(
            ValidCreate() with
            {
                CountryCode = "ae",
                TimeZoneId = "Europe/Warsaw"
            });

        Assert.True(result.Succeeded);

        var school = Assert.Single(repository.Schools);

        Assert.Equal("AE", school.CountryCode);
        Assert.Equal("Asia/Dubai", school.TimeZoneId);
    }

    [Fact]
    public async Task CreateAsync_RejectsUnsupportedCountry()
    {
        var repository = new FakeSchoolRepository();
        var service = new SchoolManagementService(repository);

        var result = await service.CreateAsync(
            ValidCreate() with { CountryCode = "US" });

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Errors,
            error =>
                error.Code == SchoolErrorCode.InvalidCountryCode);
        Assert.Empty(repository.Schools);
    }

    [Fact]
    public async Task UpdateAsync_ChangingCountry_DerivesTimeZone()
    {
        var repository = new FakeSchoolRepository();
        var school = NewSchool(
            "School",
            "WAW-001",
            SchoolStatus.Active);

        repository.Seed(school);

        var service = new SchoolManagementService(repository);

        var result = await service.UpdateAsync(
            new UpdateSchoolRequest(
                school.Id,
                "School",
                "AE",
                "Dubai",
                "school@example.com",
                "en",
                "Europe/Warsaw",
                school.RowVersion.ToArray()));

        Assert.True(result.Succeeded);
        Assert.Equal("AE", school.CountryCode);
        Assert.Equal("Asia/Dubai", school.TimeZoneId);
    }

    [Fact]
    public async Task SuspendReactivateArchive_ValidTransitionsSucceed()
    {
        var repository = new FakeSchoolRepository();
        var school = NewSchool(
            "School",
            "WAW-001",
            SchoolStatus.Active);

        repository.Seed(school);

        var service = new SchoolManagementService(repository);

        var suspend = await service.ChangeStatusAsync(
            new(
                school.Id,
                SchoolStatus.Suspended,
                school.RowVersion.ToArray()));

        Assert.True(suspend.Succeeded);
        Assert.Equal(SchoolStatus.Suspended, school.Status);

        var reactivate = await service.ChangeStatusAsync(
            new(
                school.Id,
                SchoolStatus.Active,
                school.RowVersion.ToArray()));

        Assert.True(reactivate.Succeeded);
        Assert.Equal(SchoolStatus.Active, school.Status);

        var archive = await service.ChangeStatusAsync(
            new(
                school.Id,
                SchoolStatus.Archived,
                school.RowVersion.ToArray()));

        Assert.True(archive.Succeeded);
        Assert.Equal(SchoolStatus.Archived, school.Status);
        Assert.NotNull(school.ArchivedAtUtc);
    }

    [Fact]
    public async Task ArchivedSchool_CannotBeReactivated()
    {
        var repository = new FakeSchoolRepository();
        var school = NewSchool(
            "School",
            "WAW-001",
            SchoolStatus.Archived);

        repository.Seed(school);

        var service = new SchoolManagementService(repository);

        var result = await service.ChangeStatusAsync(
            new(
                school.Id,
                SchoolStatus.Active,
                school.RowVersion.ToArray()));

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Errors,
            error =>
                error.Code ==
                SchoolErrorCode.InvalidStatusTransition);
    }

    [Fact]
    public async Task ArchivedSchool_CannotBeEdited()
    {
        var repository = new FakeSchoolRepository();
        var school = NewSchool(
            "School",
            "WAW-001",
            SchoolStatus.Archived);

        repository.Seed(school);

        var service = new SchoolManagementService(repository);

        var result = await service.UpdateAsync(
            new UpdateSchoolRequest(
                school.Id,
                "Changed",
                "PL",
                "Warsaw",
                "school@example.com",
                "en",
                "Europe/Warsaw",
                school.RowVersion.ToArray()));

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Errors,
            error =>
                error.Code ==
                SchoolErrorCode.ArchivedCannotEdit);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsConcurrencyConflict()
    {
        var repository = new FakeSchoolRepository
        {
            ForceConcurrencyConflict = true
        };

        var school = NewSchool(
            "School",
            "WAW-001",
            SchoolStatus.Active);

        repository.Seed(school);

        var service = new SchoolManagementService(repository);

        var result = await service.UpdateAsync(
            new UpdateSchoolRequest(
                school.Id,
                "Changed",
                "PL",
                "Warsaw",
                "school@example.com",
                "en",
                "Europe/Warsaw",
                school.RowVersion.ToArray()));

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Errors,
            error =>
                error.Code ==
                SchoolErrorCode.ConcurrencyConflict);
    }

    private static CreateSchoolRequest ValidCreate() =>
        new(
            "Warsaw School",
            "WAW-001",
            "PL",
            "Warsaw",
            "school@example.com",
            "en",
            "Europe/Warsaw");

    private static School NewSchool(
        string name,
        string code,
        SchoolStatus status) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            SchoolCode = code,
            NormalizedSchoolCode = code,
            Status = status,
            CountryCode = "PL",
            City = "Warsaw",
            ContactEmail = "school@example.com",
            DefaultCulture = "en",
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            ArchivedAtUtc =
                status == SchoolStatus.Archived
                    ? DateTime.UtcNow
                    : null,
            RowVersion = BitConverter.GetBytes(1L)
        };

    private sealed class FakeSchoolRepository : ISchoolRepository
    {
        private long _version = 1;

        public List<School> Schools { get; } = [];

        public bool ForceConcurrencyConflict { get; init; }

        public void Seed(School school)
        {
            Schools.Add(school);
        }

        public Task<IReadOnlyList<School>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<School>>(
                Schools.OrderBy(school => school.Name).ToArray());

        public Task<School?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Schools.SingleOrDefault(school => school.Id == id));

        public Task<School?> GetForUpdateAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Schools.SingleOrDefault(school => school.Id == id));

        public Task<bool> ExistsByNormalizedCodeAsync(
            string normalizedSchoolCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Schools.Any(
                    school =>
                        school.NormalizedSchoolCode ==
                        normalizedSchoolCode));

        public Task AddAsync(
            School school,
            CancellationToken cancellationToken = default)
        {
            Schools.Add(school);
            return Task.CompletedTask;
        }

        public Task<SchoolRepositoryWriteResult> SaveAsync(
            School school,
            byte[]? expectedRowVersion,
            CancellationToken cancellationToken = default)
        {
            if (ForceConcurrencyConflict)
            {
                return Task.FromResult(
                    SchoolRepositoryWriteResult.ConcurrencyConflict);
            }

            school.RowVersion =
                BitConverter.GetBytes(++_version);

            return Task.FromResult(
                SchoolRepositoryWriteResult.Success);
        }
    }
}
