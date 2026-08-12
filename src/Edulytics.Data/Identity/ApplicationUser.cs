using System.ComponentModel.DataAnnotations.Schema;
using Edulytics.Core.Entities;
using Microsoft.AspNetCore.Identity;

namespace Edulytics.Data.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid? SchoolId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(SchoolId))]
    public School? School { get; set; }
}
