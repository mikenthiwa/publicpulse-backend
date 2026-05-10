using System.Security.Claims;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Web.Features.Auth;
using Web.Features.Categories;
using Web.Features.Reports;
using Web.Infrastructure.Persistence;

namespace Web.UnitTests.Features.Reports;

public sealed class ReportServiceTests
{
    [Fact]
    public async Task CreateAsync_WithMissingTitle_ShouldThrowArgumentException()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateUser();
        dbContext.Users.Add(user);
        dbContext.Categories.Add(CreateCategory());
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new ReportService(dbContext, new StubReportImageUploadService());

        var action = async () => await service.CreateAsync(
            new CreateReportRequest(
                "",
                "Description",
                Category.RoadsId,
                "https://example.com/photo.jpg",
                "Nairobi",
                "Kenyatta Avenue"),
            CreatePrincipal(user.Id),
            CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenUserIsNotCreator_ShouldThrowUnauthorizedAccessException()
    {
        await using var dbContext = CreateDbContext();
        var creator = CreateUser("creator@example.com");
        var otherUser = CreateUser("other@example.com");
        var category = CreateCategory();
        var report = new Report
        {
            Title = "Pothole",
            Description = "Large pothole",
            CategoryId = category.Id,
            PhotoUrl = "https://example.com/photo.jpg",
            County = "Nairobi",
            RoadName = "Kenyatta Avenue",
            CreatedByUserId = creator.Id
        };
        dbContext.Users.AddRange(creator, otherUser);
        dbContext.Categories.Add(category);
        dbContext.Reports.Add(report);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new ReportService(dbContext, new StubReportImageUploadService());

        var action = async () => await service.UpdateStatusAsync(
            report.Id,
            new UpdateReportStatusRequest(ReportStatus.InProgress),
            CreatePrincipal(otherUser.Id),
            CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
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

    private static ClaimsPrincipal CreatePrincipal(Guid userId)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            authenticationType: "Test");

        return new ClaimsPrincipal(identity);
    }

    private sealed class StubReportImageUploadService : IReportImageUploadService
    {
        public Task<ReportImageUploadUrlResponse> CreateUploadUrlAsync(
            CreateReportImageUploadUrlRequest request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task MarkIssuedImageAsUsedAsync(
            string imageUrl,
            Guid userId,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
