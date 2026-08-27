using System.ComponentModel.DataAnnotations;
using Edulytics.Services.Users;
using Edulytics.Services.StudentSetup;

namespace Edulytics.Web.ViewModels.SchoolUsers;

public sealed class SchoolUserListViewModel
{
    public required SchoolUserManagementContext Context
    {
        get;
        init;
    }

    public IReadOnlyList<SchoolUserListItem> Users
    {
        get;
        init;
    } = [];
}

public sealed class SchoolUserCreateViewModel
{
    public Guid SchoolId { get; set; }

    [Required(ErrorMessage = "UserEmailRequired")]
    [EmailAddress(ErrorMessage = "UserEmailInvalid")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "UserRoleRequired")]
    public string Role { get; set; } = string.Empty;

    public IReadOnlyList<SchoolUserRoleOptionViewModel>
        RoleOptions { get; set; } = [];
}

public sealed record SchoolUserRoleOptionViewModel(
    string Value,
    string ResourceKey);

public sealed class SchoolUserDetailsViewModel
{
    public required SchoolUserDetails User
    {
        get;
        init;
    }

    public IReadOnlyList<SchoolUserRoleOptionViewModel>
        RoleOptions { get; init; } = [];

    public StudentRoleProvisioningContext?
        StudentSetup { get; init; }
}

public sealed class SchoolHomeViewModel
{
    public required string SchoolName { get; init; }
    public required string Role { get; init; }
    public bool CanManageUsers { get; init; }
    public bool CanManageAssessments { get; init; }
    public bool CanViewAnalytics { get; init; }
    public bool CanViewReports { get; init; }
}
