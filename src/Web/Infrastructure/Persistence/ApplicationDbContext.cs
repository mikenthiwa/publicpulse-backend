using Microsoft.EntityFrameworkCore;
using Web.Features.Auth;
using Web.Features.Categories;
using Web.Features.Reports;

namespace Web.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Report> Reports => Set<Report>();

    public DbSet<ReportImageUpload> ReportImageUploads => Set<ReportImageUpload>();

    public DbSet<ReportConfirmation> ReportConfirmations => Set<ReportConfirmation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(builder =>
        {
            builder.Property(user => user.Email)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(user => user.PasswordHash)
                .IsRequired();

            builder.HasIndex(user => user.Email)
                .IsUnique();
        });

        modelBuilder.Entity<Category>(builder =>
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
        });

        modelBuilder.Entity<Report>(builder =>
        {
            builder.Property(report => report.Title)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(report => report.Description)
                .HasMaxLength(2000)
                .IsRequired();

            builder.Property(report => report.PhotoUrl)
                .HasMaxLength(2048)
                .IsRequired();

            builder.Property(report => report.County)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(report => report.RoadName)
                .HasMaxLength(200)
                .IsRequired();

            builder.HasOne(report => report.Category)
                .WithMany(category => category.Reports)
                .HasForeignKey(report => report.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(report => report.CreatedByUser)
                .WithMany()
                .HasForeignKey(report => report.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReportImageUpload>(builder =>
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
        });

        modelBuilder.Entity<ReportConfirmation>(builder =>
        {
            builder.HasOne(confirmation => confirmation.Report)
                .WithMany(report => report.Confirmations)
                .HasForeignKey(confirmation => confirmation.ReportId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
