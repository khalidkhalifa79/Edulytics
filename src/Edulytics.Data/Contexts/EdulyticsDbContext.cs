using Edulytics.Core.Entities;
using Edulytics.Data.Configurations;
using Edulytics.Data.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Contexts;

public class EdulyticsDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public EdulyticsDbContext(DbContextOptions<EdulyticsDbContext> options)
        : base(options)
    {
    }

    public DbSet<School> Schools => Set<School>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new SchoolConfiguration());
        builder.ApplyConfiguration(new ApplicationUserConfiguration());

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("AspNetRoles");
        });
    }
}
