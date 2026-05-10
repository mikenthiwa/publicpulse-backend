using FluentAssertions;
using Web.Endpoints;

namespace Web.UnitTests.Features;

public sealed class HealthStatusTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        var checkedAtUtc = DateTimeOffset.UtcNow;

        var status = new HealthStatus(
            Status: "Healthy",
            DatabaseConfigured: true,
            DatabaseConnected: true,
            CheckedAtUtc: checkedAtUtc);

        status.Status.Should().Be("Healthy");
        status.DatabaseConfigured.Should().BeTrue();
        status.DatabaseConnected.Should().BeTrue();
        status.CheckedAtUtc.Should().Be(checkedAtUtc);
    }
}
