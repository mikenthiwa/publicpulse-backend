using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Web.Domain.Entities;

namespace Web.Infrastructure.Persistence.Configuration;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(category => category.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(category => category.Description)
            .HasMaxLength(500);

        builder.HasIndex(category => category.Name)
            .IsUnique();

        builder.HasData(
            new Category
            {
                Id = Category.RoadsId,
                Name = "Roads",
                Description = "Damaged roads, potholes, and road surface issues."
            },
            new Category
            {
                Id = Category.DrainageId,
                Name = "Drainage",
                Description = "Blocked drainage, flooding, and storm water issues."
            },
            new Category
            {
                Id = Category.StreetLightsId,
                Name = "Street Lights",
                Description = "Broken or missing public street lighting."
            },
            new Category
            {
                Id = Category.BridgesId,
                Name = "Bridges",
                Description = "Damaged bridges and related public safety issues."
            });
    }
}
