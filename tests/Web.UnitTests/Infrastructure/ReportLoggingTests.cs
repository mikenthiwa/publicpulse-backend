using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Web.Domain.Entities;
using Web.Features.Reports;
using Web.Features.Reports.CreateReport;
using Web.Infrastructure.Identity;
using Web.Infrastructure.Persistence;
using Web.Tests.Logging;

namespace Web.UnitTests.Infrastructure;

public sealed class ReportLoggingTests
{
    [Theory]
    [InlineData(false, false, 1)]
    [InlineData(true, false, 0)]
    [InlineData(false, true, 0)]
    public async Task ReportCreated_RequiresSuccessfulSave(bool missingCategory, bool failSave, int expected)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString());
        if (failSave) options.AddInterceptors(new FailedSave());
        await using var db = new ApplicationDbContext(options.Options);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        using var provider = new CapturingLoggerProvider();
        using var factory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        var handler = new CreateReportHandler(db, Substitute.For<IReportImageCloudinaryService>(),
            Substitute.For<ICurrentUser>(), factory.CreateLogger<CreateReportHandler>());
        var request = new CreateReportRequest("private description", missingCategory ? Guid.NewGuid() : Category.RoadsId,
            [], "Nairobi", "Road");
        if (failSave)
            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(request, TestContext.Current.CancellationToken));
        else await handler.HandleAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(expected, provider.Entries.Count);
        if (expected == 1)
        {
            var entry = Assert.Single(provider.Entries);
            Assert.Equal("ReportCreated", entry.EventId.Name);
            Assert.Equal((await db.Reports.SingleAsync(TestContext.Current.CancellationToken)).Id, entry.Properties["ReportId"]);
            Assert.DoesNotContain("private description", string.Join(" ", entry.Properties.Values));
        }
    }

    private sealed class FailedSave : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Synthetic save failure");
    }
}
