using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Web.Domain.Entities;

namespace Web.Infrastructure.Persistence.Configuration;

public sealed class ReportImageUploadConfiguration : IEntityTypeConfiguration<ReportImageUpload>
{
    public void Configure(EntityTypeBuilder<ReportImageUpload> builder)
    {
        builder.Property(upload => upload.ImageKey)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(upload => upload.ImageUrl)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(upload => upload.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(upload => upload.OriginalFileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(upload => upload.ImageKey)
            .IsUnique();

        builder.HasIndex(upload => upload.ImageUrl)
            .IsUnique();

        builder.HasOne(upload => upload.CreatedByUser)
            .WithMany()
            .HasForeignKey(upload => upload.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
