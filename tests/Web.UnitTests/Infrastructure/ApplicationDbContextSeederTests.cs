using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Web.Infrastructure.Persistence;

namespace Web.UnitTests.Infrastructure;

public sealed class ApplicationDbContextSeederTests
{
    [Fact]
    public async Task SeedAsync_WhenCalledTwice_ShouldNotDuplicateOrOverwriteDemoData()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var seeder = new ApplicationDbContextSeeder(
            dbContext,
            new PasswordHasher<User>(),
            NullLogger<ApplicationDbContextSeeder>.Instance);

        await seeder.SeedAsync();
        var report = await dbContext.Reports.FirstAsync(TestContext.Current.CancellationToken);
        report.Description = "Keep this operator edit.";
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await seeder.SeedAsync();

        (await dbContext.Users.CountAsync(TestContext.Current.CancellationToken)).Should().Be(2);
        (await dbContext.Reports.CountAsync(TestContext.Current.CancellationToken)).Should().Be(4);
        (await dbContext.ReportImages.CountAsync(TestContext.Current.CancellationToken)).Should().Be(3);
        (await dbContext.ReportConfirmations.CountAsync(TestContext.Current.CancellationToken)).Should().Be(4);
        (await dbContext.Reports.SingleAsync(
            seededReport => seededReport.Id == report.Id,
            TestContext.Current.CancellationToken))
            .Description.Should().Be("Keep this operator edit.");
    }

    [Fact]
    public async Task SeedAsync_WhenDatabaseContainsExistingData_ShouldPreserveIt()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var existingUser = new User
        {
            Email = "existing@example.com",
            PasswordHash = "existing-password-hash"
        };
        var existingReport = new Report
        {
            Description = "Existing report that must be preserved.",
            CategoryId = Category.RoadsId,
            County = "Nairobi",
            RoadName = "Existing Road",
            CreatedBy = existingUser.Id
        };
        dbContext.Users.Add(existingUser);
        dbContext.Reports.Add(existingReport);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var seeder = new ApplicationDbContextSeeder(
            dbContext,
            new PasswordHasher<User>(),
            NullLogger<ApplicationDbContextSeeder>.Instance);

        await seeder.SeedAsync();

        var persistedUser = await dbContext.Users.SingleAsync(
            user => user.Id == existingUser.Id,
            TestContext.Current.CancellationToken);
        var persistedReport = await dbContext.Reports.SingleAsync(
            report => report.Id == existingReport.Id,
            TestContext.Current.CancellationToken);
        persistedUser.Email.Should().Be(existingUser.Email);
        persistedUser.PasswordHash.Should().Be(existingUser.PasswordHash);
        persistedReport.Description.Should().Be(existingReport.Description);
        persistedReport.RoadName.Should().Be(existingReport.RoadName);
        (await dbContext.Users.CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
        (await dbContext.Reports.CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"application-db-context-seeder-tests-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
