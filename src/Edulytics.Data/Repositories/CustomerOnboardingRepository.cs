using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Onboarding;
using Edulytics.Data.Contexts;
using Edulytics.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Repositories;

public sealed class CustomerOnboardingRepository : ICustomerOnboardingRepository
{
    private readonly EdulyticsDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<ApplicationRole> _roles;

    public CustomerOnboardingRepository(
        EdulyticsDbContext db,
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRole> roles)
    {
        _db = db;
        _users = users;
        _roles = roles;
    }

    public Task<bool> ExistsOpenByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default) =>
        _db.DemoRequests.AsNoTracking().AnyAsync(
            x => x.NormalizedWorkEmail == normalizedEmail &&
                 x.Status != DemoRequestStatus.Won &&
                 x.Status != DemoRequestStatus.Lost,
            cancellationToken);

    public async Task<IReadOnlyList<DemoRequest>> ListRequestsAsync(
        CancellationToken cancellationToken = default) =>
        await _db.DemoRequests.AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);

    public Task<DemoRequest?> GetRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default) =>
        _db.DemoRequests.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == requestId,
            cancellationToken);

    public Task<DemoAccess?> GetDemoAccessByRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default) =>
        _db.DemoAccesses.AsNoTracking().SingleOrDefaultAsync(
            x => x.DemoRequestId == requestId,
            cancellationToken);

    public Task<DemoAccess?> GetDemoAccessBySchoolAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default) =>
        _db.DemoAccesses.AsNoTracking().SingleOrDefaultAsync(
            x => x.SchoolId == schoolId,
            cancellationToken);

    public async Task<CustomerOnboardingWriteResult> AddRequestAsync(
        DemoRequest request,
        CancellationToken cancellationToken = default)
    {
        _db.DemoRequests.Add(request);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return CustomerOnboardingWriteResult.Success();
        }
        catch (DbUpdateException)
        {
            return CustomerOnboardingWriteResult.Failure(
                CustomerOnboardingPersistenceError.PersistenceError);
        }
    }

    public async Task<CustomerOnboardingWriteResult> SaveRequestAsync(
        DemoRequest request,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default)
    {
        _db.DemoRequests.Update(request);
        _db.Entry(request).Property(x => x.RowVersion).OriginalValue =
            expectedRowVersion.ToArray();
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return CustomerOnboardingWriteResult.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            _db.Entry(request).State = EntityState.Detached;
            return CustomerOnboardingWriteResult.Failure(
                CustomerOnboardingPersistenceError.ConcurrencyConflict);
        }
        catch (DbUpdateException)
        {
            _db.Entry(request).State = EntityState.Detached;
            return CustomerOnboardingWriteResult.Failure(
                CustomerOnboardingPersistenceError.PersistenceError);
        }
    }

    public async Task<CustomerOnboardingWriteResult> SaveDemoAccessAsync(
        DemoAccess access,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default)
    {
        _db.DemoAccesses.Update(access);
        _db.Entry(access).Property(x => x.RowVersion).OriginalValue =
            expectedRowVersion.ToArray();
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return CustomerOnboardingWriteResult.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            _db.Entry(access).State = EntityState.Detached;
            return CustomerOnboardingWriteResult.Failure(
                CustomerOnboardingPersistenceError.ConcurrencyConflict);
        }
        catch (DbUpdateException)
        {
            _db.Entry(access).State = EntityState.Detached;
            return CustomerOnboardingWriteResult.Failure(
                CustomerOnboardingPersistenceError.PersistenceError);
        }
    }

    public async Task<CustomerOnboardingProvisionResult> CreateDemoAsync(
        Guid requestId,
        byte[] expectedRequestRowVersion,
        DateTime startsAtUtc,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            var request = await _db.DemoRequests.SingleOrDefaultAsync(
                x => x.Id == requestId,
                cancellationToken);

            if (request is null)
                return CustomerOnboardingProvisionResult.Failure(
                    CustomerOnboardingPersistenceError.NotFound);

            if (!request.RowVersion.SequenceEqual(expectedRequestRowVersion))
                return CustomerOnboardingProvisionResult.Failure(
                    CustomerOnboardingPersistenceError.ConcurrencyConflict);

            var existing = await _db.DemoAccesses.AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.DemoRequestId == requestId,
                    cancellationToken);

            if (existing is not null)
                return await ExistingProvisionResultAsync(
                    request,
                    existing.SchoolId,
                    existing.SchoolAdminUserId);

            if (!await _roles.RoleExistsAsync(RoleNames.SchoolAdmin))
                return CustomerOnboardingProvisionResult.Failure(
                    CustomerOnboardingPersistenceError.RoleUnavailable);

            var schoolCode = $"DEMO-{request.Id:N}"[..13].ToUpperInvariant();
            var school = new School
            {
                Id = Guid.NewGuid(),
                Name = $"Demo - {request.SchoolName}",
                SchoolCode = schoolCode,
                NormalizedSchoolCode = schoolCode,
                Status = SchoolStatus.Active,
                CountryCode = request.CountryCode,
                City = request.City,
                ContactEmail = request.WorkEmail,
                DefaultCulture = DefaultCulture(request.CountryCode),
                TimeZoneId = DefaultTimeZone(request.CountryCode),
                CreatedAtUtc = startsAtUtc,
                UpdatedAtUtc = startsAtUtc,
                RowVersion = []
            };

            _db.Schools.Add(school);
            await _db.SaveChangesAsync(cancellationToken);

            var adminResult = await CreateSchoolAdminAsync(
                school.Id,
                request.WorkEmail,
                startsAtUtc);

            if (!adminResult.Succeeded || adminResult.User is null)
            {
                if (transaction is not null)
                    await transaction.RollbackAsync(cancellationToken);
                else
                {
                    _db.Schools.Remove(school);
                    await _db.SaveChangesAsync(cancellationToken);
                }

                return CustomerOnboardingProvisionResult.Failure(adminResult.Error);
            }

            var access = new DemoAccess
            {
                Id = Guid.NewGuid(),
                DemoRequestId = request.Id,
                SchoolId = school.Id,
                SchoolAdminUserId = adminResult.User.Id,
                StartsAtUtc = startsAtUtc,
                ExpiresAtUtc = expiresAtUtc,
                CreatedAtUtc = startsAtUtc,
                UpdatedAtUtc = startsAtUtc,
                RowVersion = []
            };

            _db.DemoAccesses.Add(access);
            request.DemoSchoolId = school.Id;
            request.UpdatedAtUtc = startsAtUtc;
            _db.Entry(request).Property(x => x.RowVersion).OriginalValue =
                expectedRequestRowVersion.ToArray();

            await _db.SaveChangesAsync(cancellationToken);

            var token = await _users.GeneratePasswordResetTokenAsync(adminResult.User);

            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return CustomerOnboardingProvisionResult.Success(
                school.Id,
                adminResult.User.Id,
                token,
                request.WorkEmail,
                school.Name,
                school.DefaultCulture);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            return CustomerOnboardingProvisionResult.Failure(
                CustomerOnboardingPersistenceError.ConcurrencyConflict);
        }
        catch (DbUpdateException)
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            return CustomerOnboardingProvisionResult.Failure(
                CustomerOnboardingPersistenceError.PersistenceError);
        }
    }

    public async Task<CustomerOnboardingProvisionResult> ProvisionCustomerAsync(
        Guid requestId,
        byte[] expectedRequestRowVersion,
        string normalizedSchoolCode,
        string defaultCulture,
        string timeZoneId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            var request = await _db.DemoRequests.SingleOrDefaultAsync(
                x => x.Id == requestId,
                cancellationToken);

            if (request is null)
                return CustomerOnboardingProvisionResult.Failure(
                    CustomerOnboardingPersistenceError.NotFound);

            if (!request.RowVersion.SequenceEqual(expectedRequestRowVersion))
                return CustomerOnboardingProvisionResult.Failure(
                    CustomerOnboardingPersistenceError.ConcurrencyConflict);

            if (request.ProvisionedSchoolId.HasValue &&
                request.ProvisionedSchoolAdminUserId.HasValue)
            {
                return await ExistingProvisionResultAsync(
                    request,
                    request.ProvisionedSchoolId.Value,
                    request.ProvisionedSchoolAdminUserId.Value);
            }

            var duplicateCode = await _db.Schools.AsNoTracking().AnyAsync(
                x => x.NormalizedSchoolCode == normalizedSchoolCode &&
                     (!request.DemoSchoolId.HasValue || x.Id != request.DemoSchoolId.Value),
                cancellationToken);

            if (duplicateCode)
                return CustomerOnboardingProvisionResult.Failure(
                    CustomerOnboardingPersistenceError.DuplicateSchoolCode);

            School school;
            ApplicationUser admin;

            if (request.DemoSchoolId.HasValue)
            {
                school = await _db.Schools.SingleOrDefaultAsync(
                    x => x.Id == request.DemoSchoolId.Value,
                    cancellationToken)
                    ?? throw new InvalidOperationException("Demo school not found");

                var access = await _db.DemoAccesses.SingleOrDefaultAsync(
                    x => x.DemoRequestId == request.Id,
                    cancellationToken)
                    ?? throw new InvalidOperationException("Demo access not found");

                admin = await _users.FindByIdAsync(access.SchoolAdminUserId.ToString())
                    ?? throw new InvalidOperationException("Demo SchoolAdmin not found");

                school.Name = request.SchoolName;
                school.SchoolCode = normalizedSchoolCode;
                school.NormalizedSchoolCode = normalizedSchoolCode;
                school.Status = SchoolStatus.Suspended;
                school.CountryCode = request.CountryCode;
                school.City = request.City;
                school.ContactEmail = request.WorkEmail;
                school.DefaultCulture = defaultCulture;
                school.TimeZoneId = timeZoneId;
                school.UpdatedAtUtc = utcNow;

                access.ConvertedAtUtc = utcNow;
                access.UpdatedAtUtc = utcNow;
            }
            else
            {
                if (!await _roles.RoleExistsAsync(RoleNames.SchoolAdmin))
                    return CustomerOnboardingProvisionResult.Failure(
                        CustomerOnboardingPersistenceError.RoleUnavailable);

                school = new School
                {
                    Id = Guid.NewGuid(),
                    Name = request.SchoolName,
                    SchoolCode = normalizedSchoolCode,
                    NormalizedSchoolCode = normalizedSchoolCode,
                    Status = SchoolStatus.Suspended,
                    CountryCode = request.CountryCode,
                    City = request.City,
                    ContactEmail = request.WorkEmail,
                    DefaultCulture = defaultCulture,
                    TimeZoneId = timeZoneId,
                    CreatedAtUtc = utcNow,
                    UpdatedAtUtc = utcNow,
                    RowVersion = []
                };

                _db.Schools.Add(school);
                await _db.SaveChangesAsync(cancellationToken);

                var adminResult = await CreateSchoolAdminAsync(
                    school.Id,
                    request.WorkEmail,
                    utcNow);

                if (!adminResult.Succeeded || adminResult.User is null)
                {
                    if (transaction is not null)
                        await transaction.RollbackAsync(cancellationToken);
                    else
                    {
                        _db.Schools.Remove(school);
                        await _db.SaveChangesAsync(cancellationToken);
                    }
                    return CustomerOnboardingProvisionResult.Failure(adminResult.Error);
                }

                admin = adminResult.User;
            }

            request.ProvisionedSchoolId = school.Id;
            request.ProvisionedSchoolAdminUserId = admin.Id;
            request.UpdatedAtUtc = utcNow;
            _db.Entry(request).Property(x => x.RowVersion).OriginalValue =
                expectedRequestRowVersion.ToArray();

            await _db.SaveChangesAsync(cancellationToken);
            var token = await _users.GeneratePasswordResetTokenAsync(admin);

            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return CustomerOnboardingProvisionResult.Success(
                school.Id,
                admin.Id,
                token,
                request.WorkEmail,
                school.Name,
                school.DefaultCulture);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            return CustomerOnboardingProvisionResult.Failure(
                CustomerOnboardingPersistenceError.ConcurrencyConflict);
        }
        catch (DbUpdateException)
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            return CustomerOnboardingProvisionResult.Failure(
                CustomerOnboardingPersistenceError.PersistenceError);
        }
    }

    private async Task<CustomerOnboardingProvisionResult> ExistingProvisionResultAsync(
        DemoRequest request,
        Guid schoolId,
        Guid userId)
    {
        var school = await _db.Schools.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == schoolId);
        var user = await _users.FindByIdAsync(userId.ToString());

        if (school is null || user is null)
            return CustomerOnboardingProvisionResult.Failure(
                CustomerOnboardingPersistenceError.NotFound);

        var token = await _users.GeneratePasswordResetTokenAsync(user);
        return CustomerOnboardingProvisionResult.Success(
            school.Id,
            user.Id,
            token,
            request.WorkEmail,
            school.Name,
            school.DefaultCulture);
    }

    private async Task<CreateAdminResult> CreateSchoolAdminAsync(
        Guid schoolId,
        string email,
        DateTime utcNow)
    {
        if (await _users.FindByEmailAsync(email) is not null)
            return CreateAdminResult.Failure(
                CustomerOnboardingPersistenceError.DuplicateEmail);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            UserName = email,
            Email = email,
            EmailConfirmed = false,
            IsActive = true,
            LockoutEnabled = true,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        var create = await _users.CreateAsync(user);
        if (!create.Succeeded)
        {
            return CreateAdminResult.Failure(
                create.Errors.Any(x => x.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))
                    ? CustomerOnboardingPersistenceError.DuplicateEmail
                    : CustomerOnboardingPersistenceError.PersistenceError);
        }

        var role = await _users.AddToRoleAsync(user, RoleNames.SchoolAdmin);
        if (!role.Succeeded)
        {
            await _users.DeleteAsync(user);
            return CreateAdminResult.Failure(
                CustomerOnboardingPersistenceError.RoleUnavailable);
        }

        return CreateAdminResult.Success(user);
    }

    private static string DefaultCulture(string countryCode) =>
        string.Equals(countryCode, "PL", StringComparison.OrdinalIgnoreCase)
            ? "pl"
            : "en";

    private static string DefaultTimeZone(string countryCode) =>
        countryCode.ToUpperInvariant() switch
        {
            "PL" => "Europe/Warsaw",
            "AE" => "Asia/Dubai",
            _ => "UTC"
        };

    private sealed record CreateAdminResult(
        bool Succeeded,
        ApplicationUser? User,
        CustomerOnboardingPersistenceError Error)
    {
        public static CreateAdminResult Success(ApplicationUser user) =>
            new(true, user, CustomerOnboardingPersistenceError.None);
        public static CreateAdminResult Failure(CustomerOnboardingPersistenceError error) =>
            new(false, null, error);
    }
}
