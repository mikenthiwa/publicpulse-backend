using System.Security.Claims;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Web.Features.Auth;
using Web.Features.Categories;
using Web.Features.Reports;
using Web.Infrastructure.Identity;
using Web.Infrastructure.Persistence;

namespace Web.UnitTests.Features.Reports;

public sealed class ReportServiceTests
{
    [Fact]
    public async Task UpdateStatusAsync_WhenUserIsNotCreator_ShouldThrowUnauthorizedAccessException()
    {
        await using var dbContext = CreateDbContext();
        var creator = CreateUser("creator@example.com");
        var otherUser = CreateUser("other@example.com");
        var category = CreateCategory();
        var report = new Report
        {
            Description = "Large pothole",
            CategoryId = category.Id,
            County = "Nairobi",
            RoadName = "Kenyatta Avenue",
            CreatedBy = creator.Id
        };
        dbContext.Users.AddRange(creator, otherUser);
        dbContext.Categories.Add(category);
        dbContext.Reports.Add(report);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new ReportService(dbContext, new TestCurrentUser(otherUser.Id));

        var action = async () => await service.UpdateStatusAsync(
            report.Id,
            new UpdateReportStatusRequest(ReportStatus.InProgress),
            CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenUserIsCreator_ShouldUpdateStatus()
    {
        await using var dbContext = CreateDbContext();
        var creator = CreateUser("creator@example.com");
        var category = CreateCategory();
        var report = new Report
        {
            Description = "Large pothole",
            CategoryId = category.Id,
            Category = category,
            County = "Nairobi",
            RoadName = "Kenyatta Avenue",
            CreatedBy = creator.Id
        };
        dbContext.Users.Add(creator);
        dbContext.Categories.Add(category);
        dbContext.Reports.Add(report);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new ReportService(dbContext, new TestCurrentUser(creator.Id));

        var updatedReport = await service.UpdateStatusAsync(
            report.Id,
            new UpdateReportStatusRequest(ReportStatus.InProgress),
            CancellationToken.None);

        updatedReport.Status.Should().Be(ReportStatus.InProgress);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"report-service-tests-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static User CreateUser(string email = "citizen@example.com")
    {
        return new User
        {
            Email = email,
            PasswordHash = "hash"
        };
    }

    private static Category CreateCategory()
    {
        return new Category
        {
            Id = Category.RoadsId,
            Name = "Roads"
        };
    }

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public ClaimsPrincipal User { get; } = CreatePrincipal(userId);

        public Guid UserId { get; } = userId;

        private static ClaimsPrincipal CreatePrincipal(Guid userId)
        {
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                authenticationType: "Test");

            return new ClaimsPrincipal(identity);
        }
    }
}
