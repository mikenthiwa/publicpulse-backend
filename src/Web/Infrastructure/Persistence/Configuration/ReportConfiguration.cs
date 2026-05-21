using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Web.Domain.Entities;

namespace Web.Infrastructure.Persistence.Configuration;

public sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("Reports");

        builder.Property(r => r.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(r => r.PhotoUrl)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(r => r.County)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.RoadName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.CreatedBy)
            .IsRequired();

        builder.HasOne(r => r.Category)
            .WithMany(category => category.Reports)
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.CreatedByUser)
            .WithMany()
            .HasForeignKey(r => r.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
