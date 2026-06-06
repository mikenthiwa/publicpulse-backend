using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Web.Common.Models;
using Web.Domain.Entities;
using Web.Features.Categories;
using Web.Features.Reports;
using Web.IntegrationTests.Helpers;
using Web.Infrastructure.Persistence;

namespace Web.IntegrationTests.Features.Reports;

public sealed class ReportEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private string? _currentUserFolder;

    public ReportEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateImageUploadSignature_WithToken_ShouldReturnSignedCloudinaryParams()
    {
        await AuthenticateAsync("signature@example.com");

        var response = await _client.PostAsync(
            "/api/Reports/images/upload-signature",
            content: null,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var signature = await ApiTestClient.ReadDataAsync<ReportImageUploadSignatureResponse>(
            response,
            TestContext.Current.CancellationToken);

        signature.CloudName.Should().Be("public-pulse");
        signature.ApiKey.Should().Be("test-api-key");
        signature.Timestamp.Should().Be(1_800_000_000);
        signature.Signature.Should().Be("test-upload-signature");
        signature.Folder.Should().StartWith("public-pulse/reports/");
        signature.UploadPreset.Should().Be("test-upload-preset");
        _currentUserFolder = signature.Folder;
    }

    [Fact]
    public async Task CreateImageUploadSignature_WithoutToken_ShouldReturnUnauthorized()
    {
        var response = await _client.PostAsync(
            "/api/Reports/images/upload-signature",
            content: null,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AssertUnauthorizedProblemDetailsAsync(response, "/api/Reports/images/upload-signature");
    }

    [Fact]
    public async Task CreateReport_WithoutToken_ShouldReturnUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/Reports",
            CreateReportRequest(),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AssertUnauthorizedProblemDetailsAsync(response, "/api/Reports");
    }

    [Fact]
    public async Task CreateReport_WithVerifiedCloudinaryImages_ShouldReturnCreatedAndHideCreatorIdentity()
    {
        await AuthenticateAsync("report-creator@example.com");

        var response = await _client.PostAsJsonAsync(
            "/api/Reports",
            CreateReportRequest(imageCount: 2),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var responseJson = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseJson.Should().NotContain("createdByUserId");
        responseJson.Should().NotContain("report-creator@example.com");

        var report = await ApiTestClient.ReadDataAsync<ReportResponse>(
            response,
            TestContext.Current.CancellationToken);

        report.Id.Should().NotBeEmpty();
        report.County.Should().Be("Nairobi");
        report.RoadName.Should().Be("Kenyatta Avenue");
        report.Latitude.Should().BeNull();
        report.Longitude.Should().BeNull();
        report.LocationLabel.Should().BeNull();
        report.LocationSource.Should().BeNull();
        report.Status.Should().Be(ReportStatus.Reported);
        report.ConfirmationCount.Should().Be(0);
        report.Images.Should().HaveCount(2);
        report.Images.Should().OnlyContain(image => image.PublicId.StartsWith(_currentUserFolder!, StringComparison.Ordinal));
        report.Images.Should().OnlyContain(image =>
            image.ImageUrl == $"https://res.cloudinary.com/public-pulse/image/upload/v123/{EscapePublicId(image.PublicId)}");
    }

    [Fact]
    public async Task CreateReport_WithOptionalCoordinateMetadata_ShouldPersistAndReturnLocationFields()
    {
        await AuthenticateAsync("location-metadata@example.com");
        var request = CreateReportRequest(
            latitude: -1.286389,
            longitude: 36.817223,
            locationLabel: "Kenyatta Avenue, Nairobi, Kenya",
            locationSource: "mapbox");

        var createResponse = await _client.PostAsJsonAsync(
            "/api/Reports",
            request,
            TestContext.Current.CancellationToken);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdReport = await ApiTestClient.ReadDataAsync<ReportResponse>(
            createResponse,
            TestContext.Current.CancellationToken);

        createdReport.Latitude.Should().Be(-1.286389);
        createdReport.Longitude.Should().Be(36.817223);
        createdReport.LocationLabel.Should().Be("Kenyatta Avenue, Nairobi, Kenya");
        createdReport.LocationSource.Should().Be("mapbox");

        _client.DefaultRequestHeaders.Authorization = null;
        var listResponse = await _client.GetAsync("/api/Reports", TestContext.Current.CancellationToken);
        var reports = await ApiTestClient.ReadDataAsync<PaginatedList<ReportListItemResponse>>(
            listResponse,
            TestContext.Current.CancellationToken);

        var listedReport = reports.Items.Should().Contain(report => report.Id == createdReport.Id).Which;
        listedReport.Latitude.Should().Be(-1.286389);
        listedReport.Longitude.Should().Be(36.817223);
        listedReport.LocationLabel.Should().Be("Kenyatta Avenue, Nairobi, Kenya");
        listedReport.LocationSource.Should().Be("mapbox");

        var detailResponse = await _client.GetAsync(
            $"/api/Reports/{createdReport.Id}",
            TestContext.Current.CancellationToken);
        var detailReport = await ApiTestClient.ReadDataAsync<ReportResponse>(
            detailResponse,
            TestContext.Current.CancellationToken);

        detailReport.Latitude.Should().Be(-1.286389);
        detailReport.Longitude.Should().Be(36.817223);
        detailReport.LocationLabel.Should().Be("Kenyatta Avenue, Nairobi, Kenya");
        detailReport.LocationSource.Should().Be("mapbox");
    }

    [Fact]
    public async Task CreateReport_WithSpecialCharactersInPublicId_ShouldReturnEscapedImageUrl()
    {
        await AuthenticateAsync("escaped-url@example.com");
        var publicId = $"{_currentUserFolder}/road image #1";

        var response = await _client.PostAsJsonAsync(
            "/api/Reports",
            CreateReportRequest(images: [new CreateReportImageRequest(publicId, "123", "valid-signature")]),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var report = await ApiTestClient.ReadDataAsync<ReportResponse>(
            response,
            TestContext.Current.CancellationToken);

        report.Images.Should().ContainSingle()
            .Which.ImageUrl.Should().Be(
                $"https://res.cloudinary.com/public-pulse/image/upload/v123/{EscapePublicId(publicId)}");
    }

    [Fact]
    public async Task CreateReport_WithDuplicatePublicIds_ShouldReturnBadRequest()
    {
        await AuthenticateAsync("duplicate-image@example.com");
        var publicId = $"{_currentUserFolder}/road-duplicate";
        var images = new[]
        {
            new CreateReportImageRequest(publicId, "123", "valid-signature"),
            new CreateReportImageRequest(publicId, "123", "valid-signature")
        };

        var response = await _client.PostAsJsonAsync(
            "/api/Reports",
            CreateReportRequest(images: images),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateReport_WithPreviouslyUsedPublicId_ShouldReturnBadRequest()
    {
        await AuthenticateAsync("reused-image@example.com");
        var publicId = $"{_currentUserFolder}/road-reused";
        var firstResponse = await _client.PostAsJsonAsync(
            "/api/Reports",
            CreateReportRequest(images: [new CreateReportImageRequest(publicId, "123", "valid-signature")]),
            TestContext.Current.CancellationToken);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondResponse = await _client.PostAsJsonAsync(
            "/api/Reports",
            CreateReportRequest(images: [new CreateReportImageRequest(publicId, "123", "valid-signature")]),
            TestContext.Current.CancellationToken);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateReport_WithInvalidCloudinarySignature_ShouldReturnBadRequest()
    {
        await AuthenticateAsync("invalid-signature@example.com");

        var response = await _client.PostAsJsonAsync(
            "/api/Reports",
            CreateReportRequest(signature: "invalid-signature"),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateReport_WithImageOutsideUserFolder_ShouldReturnBadRequest()
    {
        await AuthenticateAsync("wrong-folder@example.com");

        var response = await _client.PostAsJsonAsync(
            "/api/Reports",
            CreateReportRequest(publicIdPrefix: "public-pulse/reports/other-user"),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateReport_WithMoreThanFiveImages_ShouldReturnBadRequest()
    {
        await AuthenticateAsync("too-many-images@example.com");

        var response = await _client.PostAsJsonAsync(
            "/api/Reports",
            CreateReportRequest(imageCount: 6),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateReport_WithNoImages_ShouldReturnBadRequest()
    {
        await AuthenticateAsync("no-images@example.com");

        var response = await _client.PostAsJsonAsync(
            "/api/Reports",
            CreateReportRequest(images: []),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateReport_WithMissingImages_ShouldReturnBadRequest()
    {
        await AuthenticateAsync("missing-images@example.com");
        var request = new
        {
            Description = "A large pothole is damaging vehicles.",
            CategoryId = Category.RoadsId,
            County = "Nairobi",
            RoadName = "Kenyatta Avenue"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/Reports",
            request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListReports_ShouldBePublicAndHideCreatorIdentity()
    {
        await AuthenticateAsync("list-creator@example.com");
        var createdReport = await CreateReportAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/Reports", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseJson = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseJson.Should().NotContain("createdByUserId");
        responseJson.Should().NotContain("list-creator@example.com");

        var reports = await ApiTestClient.ReadDataAsync<PaginatedList<ReportListItemResponse>>(
            response,
            TestContext.Current.CancellationToken);

        reports.PageNumber.Should().Be(1);
        reports.PageSize.Should().Be(10);
        reports.Items.Should().Contain(report => report.Id == createdReport.Id);
    }

    [Fact]
    public async Task ListReports_WithPagingParameters_ShouldReturnRequestedPageMetadata()
    {
        var response = await _client.GetAsync(
            "/api/Reports?pageNumber=2&pageSize=1",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reports = await ApiTestClient.ReadDataAsync<PaginatedList<ReportListItemResponse>>(
            response,
            TestContext.Current.CancellationToken);

        reports.PageNumber.Should().Be(2);
        reports.PageSize.Should().Be(1);
        reports.Items.Should().HaveCountLessThanOrEqualTo(1);
    }

    [Fact]
    public async Task ListReports_WithMatchingCreatedTimes_ShouldUseIdAsTieBreaker()
    {
        const string email = "pagination-order@example.com";
        await AuthenticateAsync(email);
        var created = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var lowerId = Guid.Parse("10000000-0000-0000-0000-000000000000");
        var higherId = Guid.Parse("20000000-0000-0000-0000-000000000000");

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userId = dbContext.Users.Single(user => user.Email == email).Id;
            dbContext.Reports.AddRange(
                CreateReport(lowerId, created, userId, "Lower ID Road"),
                CreateReport(higherId, created, userId, "Higher ID Road"));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await _client.GetAsync(
            "/api/Reports?pageNumber=1&pageSize=100",
            TestContext.Current.CancellationToken);
        var reports = await ApiTestClient.ReadDataAsync<PaginatedList<ReportListItemResponse>>(
            response,
            TestContext.Current.CancellationToken);

        var tiedReportIds = reports.Items
            .Where(report => report.Created == created)
            .Select(report => report.Id);

        tiedReportIds.Should().Equal(higherId, lowerId);
    }

    [Theory]
    [InlineData("/api/Reports?pageNumber=0")]
    [InlineData("/api/Reports?pageNumber=-1")]
    [InlineData("/api/Reports?pageNumber=2147483647&pageSize=100")]
    [InlineData("/api/Reports?pageSize=0")]
    [InlineData("/api/Reports?pageSize=-1")]
    [InlineData("/api/Reports?pageSize=101")]
    public async Task ListReports_WithInvalidPagingParameters_ShouldReturnBadRequest(string requestUri)
    {
        var response = await _client.GetAsync(requestUri, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    private static Report CreateReport(
        Guid id,
        DateTimeOffset created,
        Guid createdBy,
        string roadName)
    {
        return new Report
        {
            Id = id,
            Description = "Pagination ordering test report.",
            CategoryId = Category.RoadsId,
            County = "Nairobi",
            RoadName = roadName,
            Created = created,
            CreatedBy = createdBy
        };
    }

    [Fact]
    public async Task GetReportById_ShouldBePublicAndReturnImages()
    {
        await AuthenticateAsync("detail-creator@example.com");
        var createdReport = await CreateReportAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync(
            $"/api/Reports/{createdReport.Id}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseJson = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseJson.Should().NotContain("createdByUserId");
        responseJson.Should().NotContain("detail-creator@example.com");

        var report = await ApiTestClient.ReadDataAsync<ReportResponse>(
            response,
            TestContext.Current.CancellationToken);

        report.Id.Should().Be(createdReport.Id);
        report.Images.Should().ContainSingle();
    }

    [Fact]
    public async Task ConfirmReport_ShouldBePublicAndIncreaseConfirmationCount()
    {
        await AuthenticateAsync("confirm-creator@example.com");
        var createdReport = await CreateReportAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var firstResponse = await _client.PostAsync(
            $"/api/Reports/{createdReport.Id}/confirmations",
            content: null,
            TestContext.Current.CancellationToken);
        var secondResponse = await _client.PostAsync(
            $"/api/Reports/{createdReport.Id}/confirmations",
            content: null,
            TestContext.Current.CancellationToken);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var confirmation = await ApiTestClient.ReadDataAsync<ConfirmReportResponse>(
            secondResponse,
            TestContext.Current.CancellationToken);

        confirmation.ConfirmationCount.Should().Be(2);
    }

    [Fact]
    public async Task UpdateStatus_WithoutToken_ShouldReturnUnauthorized()
    {
        await AuthenticateAsync("status-auth@example.com");
        var createdReport = await CreateReportAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PutAsJsonAsync(
            $"/api/Reports/{createdReport.Id}/status",
            new UpdateReportStatusRequest(ReportStatus.InProgress),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AssertUnauthorizedProblemDetailsAsync(response, $"/api/Reports/{createdReport.Id}/status");
    }

    [Fact]
    public async Task UpdateStatus_AsCreator_ShouldUpdateStatus()
    {
        await AuthenticateAsync("status-creator@example.com");
        var createdReport = await CreateReportAsync();

        var response = await _client.PutAsJsonAsync(
            $"/api/Reports/{createdReport.Id}/status",
            new UpdateReportStatusRequest(ReportStatus.InProgress),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var report = await ApiTestClient.ReadDataAsync<ReportResponse>(
            response,
            TestContext.Current.CancellationToken);

        report.Status.Should().Be(ReportStatus.InProgress);
        report.Images.Should().ContainSingle();
    }

    [Fact]
    public async Task UpdateStatus_AsDifferentUser_ShouldReturnForbidden()
    {
        await AuthenticateAsync("owner@example.com");
        var createdReport = await CreateReportAsync();
        await AuthenticateAsync("different-user@example.com");

        var response = await _client.PutAsJsonAsync(
            $"/api/Reports/{createdReport.Id}/status",
            new UpdateReportStatusRequest(ReportStatus.Resolved),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateStatus_ForMissingReport_ShouldReturnNotFound()
    {
        await AuthenticateAsync("missing-status@example.com");

        var response = await _client.PutAsJsonAsync(
            $"/api/Reports/{Guid.NewGuid()}/status",
            new UpdateReportStatusRequest(ReportStatus.Resolved),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task AuthenticateAsync(string email)
    {
        var auth = await ApiTestClient.RegisterAndLoginAsync(
            _client,
            email,
            "Password123!",
            TestContext.Current.CancellationToken);

        _client.SetBearerToken(auth.Token);
        var signatureResponse = await _client.PostAsync(
            "/api/Reports/images/upload-signature",
            content: null,
            TestContext.Current.CancellationToken);
        var signature = await ApiTestClient.ReadDataAsync<ReportImageUploadSignatureResponse>(
            signatureResponse,
            TestContext.Current.CancellationToken);
        _currentUserFolder = signature.Folder;
    }

    private static async Task AssertUnauthorizedProblemDetailsAsync(
        HttpResponseMessage response,
        string instance)
    {
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problemDetails = await response.Content.ReadFromJsonAsync<TestProblemDetails>(
            TestContext.Current.CancellationToken);

        problemDetails.Should().NotBeNull();
        problemDetails!.Type.Should().Be("https://tools.ietf.org/html/rfc7235#section-3.1");
        problemDetails.Title.Should().Be("Unauthorized.");
        problemDetails.Status.Should().Be((int)HttpStatusCode.Unauthorized);
        problemDetails.Detail.Should().Be("Authentication is required to access this resource.");
        problemDetails.Instance.Should().Be(instance);
        problemDetails.TraceId.Should().NotBeNullOrWhiteSpace();
    }

    private sealed record TestProblemDetails(
        string? Type,
        string? Title,
        int? Status,
        string? Detail,
        string? Instance,
        string? TraceId);

    private async Task<ReportResponse> CreateReportAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/Reports",
            CreateReportRequest(),
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        return await ApiTestClient.ReadDataAsync<ReportResponse>(
            response,
            TestContext.Current.CancellationToken);
    }

    private CreateReportRequest CreateReportRequest(
        int imageCount = 1,
        IReadOnlyList<CreateReportImageRequest>? images = null,
        string roadName = "Kenyatta Avenue",
        string signature = "valid-signature",
        string? publicIdPrefix = null,
        double? latitude = null,
        double? longitude = null,
        string? locationLabel = null,
        string? locationSource = null)
    {
        return new CreateReportRequest(
            "A large pothole is damaging vehicles.",
            Category.RoadsId,
            images ?? Enumerable.Range(1, imageCount)
                .Select(index => CreateImage(index, signature, publicIdPrefix))
                .ToArray(),
            "Nairobi",
            roadName,
            latitude,
            longitude,
            locationLabel,
            locationSource);
    }

    private CreateReportImageRequest CreateImage(
        int index,
        string signature,
        string? publicIdPrefix)
    {
        var prefix = publicIdPrefix ?? _currentUserFolder ?? "public-pulse/reports/missing-user";
        var publicId = $"{prefix}/road-{index}";

        return new CreateReportImageRequest(
            publicId,
            "123",
            signature);
    }

    private static string EscapePublicId(string publicId)
    {
        return string.Join(
            "/",
            publicId
                .Trim()
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));
    }
}
