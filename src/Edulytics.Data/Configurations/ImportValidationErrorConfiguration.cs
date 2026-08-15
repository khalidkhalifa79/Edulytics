using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class ImportValidationErrorConfiguration
    : IEntityTypeConfiguration<ImportValidationError>
{
    public void Configure(
        EntityTypeBuilder<ImportValidationError> builder)
    {
        builder.ToTable("ImportValidationErrors");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ColumnName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.RawValue)
            .HasMaxLength(500);

        builder.HasIndex(x => new
        {
            x.SchoolId,
            x.ImportBatchId,
            x.RowNumber
        });

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ImportBatch>()
            .WithMany()
            .HasForeignKey(x => new
            {
                x.SchoolId,
                x.ImportBatchId
            })
            .HasPrincipalKey(x => new
            {
                x.SchoolId,
                x.Id
            })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
