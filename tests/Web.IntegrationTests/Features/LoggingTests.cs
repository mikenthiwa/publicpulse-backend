using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Web.Domain.Entities;
using Web.Features.Reports;
using Web.IntegrationTests.Helpers;
using Web.Tests.Logging;

namespace Web.IntegrationTests.Features;

public sealed class LoggingTests
{
    [Fact]
    public async Task ConsoleFormatter_EmitsJsonWithUtcTimePropertiesAndScopes()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        Assert.Equal("json", factory.Services.GetRequiredService<IOptionsMonitor<ConsoleLoggerOptions>>().CurrentValue.FormatterName);
        var formatter = factory.Services.GetServices<ConsoleFormatter>().Single(f => f.Name == "json");
        var scopes = new LoggerExternalScopeProvider();
        using var scope = scopes.Push(new Dictionary<string, object?> { ["TraceId"] = "synthetic-trace" });
        var state = new List<KeyValuePair<string, object?>> { new("ReportId", "synthetic-report") };
        var entry = new Microsoft.Extensions.Logging.Abstractions.LogEntry<List<KeyValuePair<string, object?>>>(
            LogLevel.Information, "Test", new EventId(1003), state, null, (_, _) => "Report created");
        using var output = new StringWriter();
        formatter.Write(in entry, scopes, output);
        using var json = JsonDocument.Parse(output.ToString());
        Assert.EndsWith("Z", json.RootElement.GetProperty("Timestamp").GetString());
        Assert.Equal("synthetic-report", json.RootElement.GetProperty("State").GetProperty("ReportId").GetString());
        Assert.Equal("synthetic-trace", json.RootElement.GetProperty("Scopes")[0].GetProperty("TraceId").GetString());
    }

    [Fact]
    public async Task RequestsAndReportCreation_HaveCorrelatedEvents()
    {
        using var provider = new CapturingLoggerProvider();
        await using var factory = new TestWebApplicationFactory().WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(provider)));
        using var client = factory.CreateClient();
        await client.GetAsync("/swagger/index.html", TestContext.Current.CancellationToken);
        await client.GetAsync("/unknown?secret=private-query", TestContext.Current.CancellationToken);
        await client.PostAsync("/api/v1/Reports", null, TestContext.Current.CancellationToken);
        var auth = await ApiTestClient.RegisterAndLoginAsync(client, "logging@example.com", "Password123!", TestContext.Current.CancellationToken);
        client.SetBearerToken(auth.Token);
        var signatureResponse = await client.PostAsync("/api/v1/Reports/images/upload-signature", null, TestContext.Current.CancellationToken);
        var signature = await ApiTestClient.ReadDataAsync<ReportImageUploadSignatureResponse>(signatureResponse, TestContext.Current.CancellationToken);
        var request = new CreateReportRequest("private-description", Category.RoadsId,
            [new CreateReportImageRequest(signature.Folder + "/sample", "1", "valid-signature")], "Nairobi", "Road");
        var created = await client.PostAsJsonAsync("/api/v1/Reports", request, TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.Created, created.StatusCode);
        var rejected = await client.PostAsJsonAsync("/api/v1/Reports", request with { Description = "" }, TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, rejected.StatusCode);
        var summaries = provider.Entries.Where(e => e.EventId.Name == "RequestCompleted").ToArray();
        foreach (var status in new[] { 200, 201, 400, 401, 404 })
            Assert.Contains(summaries, e => Equals(e.Properties["StatusCode"], status));
        Assert.All(summaries.GroupBy(e => e.Scope["RequestId"]), group => Assert.Single(group));
        var report = Assert.Single(provider.Entries, e => e.EventId.Name == "ReportCreated");
        Assert.Contains(summaries, e => Equals(e.Scope["TraceId"], report.Scope["TraceId"]) && Equals(e.Properties["StatusCode"], 201));
        var serialized = JsonSerializer.Serialize(provider.Entries.Where(e => e.EventId.Id is >= 1000 and <= 1003));
        foreach (var sensitive in new[] { auth.Token, "private-description", "private-query", "valid-signature" })
            Assert.DoesNotContain(sensitive, serialized);
    }

    [Fact]
    public async Task HandledException_MatchesProblemDetailsCorrelation()
    {
        using var provider = new CapturingLoggerProvider();
        await using var factory = new ExceptionTestWebApplicationFactory().WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(provider)));
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/test/unhandled-exception", TestContext.Current.CancellationToken);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var summary = Assert.Single(provider.Entries, e => e.EventId.Name == "RequestCompleted");
        var failure = Assert.Single(provider.Entries, e => e.Exception?.Message == "Test exception.");
        Assert.Equal(500, summary.Properties["StatusCode"]);
        Assert.Equal(LogLevel.Error, summary.Level);
        Assert.Equal(body.RootElement.GetProperty("traceId").GetString(), summary.Scope["ProblemDetailsTraceId"]);
        Assert.Equal(failure.Scope["TraceId"], summary.Scope["TraceId"]);
    }
}
