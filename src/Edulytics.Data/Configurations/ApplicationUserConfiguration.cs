using Edulytics.Data.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("AspNetUsers");

        builder.Property(x => x.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.UserName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        // ASP.NET Identity's ConcurrencyStamp is the
        // authoritative optimistic-concurrency token for
        // ApplicationUser. User mutations go through
        // UserManager so Identity can return a concurrency
        // failure instead of silently overwriting state.
        builder.Property(x => x.ConcurrencyStamp)
            .IsConcurrencyToken();

        builder.HasIndex(x => x.SchoolId);

        builder.HasIndex(x => x.NormalizedEmail)
            .IsUnique()
            .HasFilter("\"NormalizedEmail\" IS NOT NULL");

        builder.HasIndex(x => x.NormalizedUserName)
            .IsUnique();

        builder.HasOne(x => x.School)
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
