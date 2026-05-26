using Microsoft.EntityFrameworkCore;
using Web.Domain.Entities;
using Web.Infrastructure.Persistence.Configuration;

namespace Web.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Report> Reports => Set<Report>();

    public DbSet<ReportImage> ReportImages => Set<ReportImage>();

    public DbSet<ReportConfirmation> ReportConfirmations => Set<ReportConfirmation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new ReportConfiguration());
        modelBuilder.ApplyConfiguration(new ReportImageConfiguration());
        modelBuilder.ApplyConfiguration(new ReportConfirmationConfiguration());
    }
}
