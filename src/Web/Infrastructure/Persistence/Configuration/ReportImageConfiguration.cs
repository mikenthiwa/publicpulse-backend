using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Web.Domain.Entities;

namespace Web.Infrastructure.Persistence.Configuration;

public sealed class ReportImageConfiguration : IEntityTypeConfiguration<ReportImage>
{
    public void Configure(EntityTypeBuilder<ReportImage> builder)
    {
        builder.ToTable("ReportImages");

        builder.Property(image => image.ImageUrl)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(image => image.PublicId)
            .HasMaxLength(512)
            .IsRequired();

        builder.HasIndex(image => image.PublicId)
            .IsUnique();

        builder.HasOne(image => image.Report)
            .WithMany(report => report.Images)
            .HasForeignKey(image => image.ReportId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
