using System.Security.Claims;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Web.Common.Models;
using Web.Features.Auth;
using Web.Features.Categories;
using Web.Features.Reports;
using Web.Features.Reports.UpdateReportStatus;
using Web.Infrastructure.Identity;
using Web.Infrastructure.Persistence;

namespace Web.UnitTests.Features.Reports;

public sealed class UpdateReportStatusHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenUserIsNotCreator_ShouldReturnForbidden()
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
        var handler = new UpdateReportStatusHandler(dbContext, new TestCurrentUser(otherUser.Id));

        var result = await handler.HandleAsync(
            report.Id,
            new UpdateReportStatusRequest(ReportStatus.InProgress),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(new ApplicationError(
            ApplicationErrorKind.Forbidden,
            "Only the report creator can update status."));
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsCreator_ShouldUpdateStatus()
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
        var handler = new UpdateReportStatusHandler(dbContext, new TestCurrentUser(creator.Id));

        var result = await handler.HandleAsync(
            report.Id,
            new UpdateReportStatusRequest(ReportStatus.InProgress),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ReportStatus.InProgress);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"update-report-status-handler-tests-{Guid.NewGuid()}")
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
