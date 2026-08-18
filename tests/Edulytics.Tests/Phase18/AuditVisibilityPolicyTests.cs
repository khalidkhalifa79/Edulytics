using Edulytics.Core.Constants;
using Edulytics.Services.Auditing;

namespace Edulytics.Tests.Phase18;

public sealed class AuditVisibilityPolicyTests
{
    [Fact]
    public void SchoolAdmin_IsRestrictedToOwnSchool()
    {
        var ownSchool = Guid.NewGuid();

        var own =
            AuditVisibilityPolicy.Resolve(
                RoleNames.SchoolAdmin,
                ownSchool,
                ownSchool);

        Assert.True(own.Allowed);
        Assert.False(own.AllSchools);
        Assert.Equal(
            ownSchool,
            own.SchoolId);

        var implicitOwn =
            AuditVisibilityPolicy.Resolve(
                RoleNames.SchoolAdmin,
                ownSchool,
                null);

        Assert.True(
            implicitOwn.Allowed);

        var other =
            AuditVisibilityPolicy.Resolve(
                RoleNames.SchoolAdmin,
                ownSchool,
                Guid.NewGuid());

        Assert.False(
            other.Allowed);
    }

    [Fact]
    public void SchoolAdmin_WithoutSchool_IsDenied()
    {
        var result =
            AuditVisibilityPolicy.Resolve(
                RoleNames.SchoolAdmin,
                null,
                null);

        Assert.False(
            result.Allowed);
    }

    [Fact]
    public void SuperAdmin_CanQueryAllOrSpecificSchool()
    {
        var all =
            AuditVisibilityPolicy.Resolve(
                RoleNames.SuperAdmin,
                null,
                null);

        Assert.True(all.Allowed);
        Assert.True(all.AllSchools);
        Assert.Null(all.SchoolId);

        var school = Guid.NewGuid();

        var specific =
            AuditVisibilityPolicy.Resolve(
                RoleNames.SuperAdmin,
                null,
                school);

        Assert.True(specific.Allowed);
        Assert.False(specific.AllSchools);
        Assert.Equal(
            school,
            specific.SchoolId);
    }

    [Fact]
    public void NonAdministrativeRoles_AreDenied()
    {
        var school = Guid.NewGuid();

        Assert.False(
            AuditVisibilityPolicy.Resolve(
                RoleNames.Teacher,
                school,
                school)
            .Allowed);

        Assert.False(
            AuditVisibilityPolicy.Resolve(
                RoleNames.Student,
                school,
                school)
            .Allowed);
    }
}
