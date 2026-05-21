using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using FluentAssertions;
using Web.Features.Categories;
using Web.Features.Reports;
using Web.IntegrationTests.Helpers;

namespace Web.IntegrationTests.Features.Reports;

public sealed class ReportEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ReportEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateReport_WithoutToken_ShouldReturnUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/Reports",
            CreateReportRequest("https://example.com/photo.jpg"),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateReport_WithToken_ShouldReturnCreatedAndHideCreatorIdentity()
    {
        await AuthenticateAsync("report-creator@example.com");
        var upload = await CreateImageUploadAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/Reports",
            CreateReportRequest(upload.ImageUrl),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var responseJson = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseJson.Should().NotContain("createdByUserId");
        responseJson.Should().NotContain("report-creator@example.com");

        var report = await ApiTestClient.ReadDataAsync<ReportResponse>(
            response,
            TestContext.Current.CancellationToken);

        report.Id.Should().NotBeEmpty();
        report.Status.Should().Be(ReportStatus.Reported);
        report.ConfirmationCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateImageUploadUrl_WithTokenAndValidMetadata_ShouldReturnUploadTarget()
    {
        await AuthenticateAsync("image-upload@example.com");

        var response = await _client.PostAsJsonAsync(
            "/api/Reports/images/upload-url",
            new CreateReportImageUploadUrlRequest("road.jpg", "image/jpeg", 12_345),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var upload = await ApiTestClient.ReadDataAsync<ReportImageUploadUrlResponse>(
            response,
            TestContext.Current.CancellationToken);

        upload.UploadUrl.Should().Contain("/api/Reports/images/uploads/");
        upload.ImageUrl.Should().Contain("/uploads/reports/");
        upload.ImageKey.Should().StartWith("reports/");
        upload.ImageKey.Should().EndWith(".jpg");
        upload.ImageKey.Should().NotContain("road.jpg");
        upload.Headers.Should().Contain("Content-Type", "image/jpeg");
    }

    [Fact]
    public async Task CreateImageUploadUrl_WithoutToken_ShouldReturnUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/Reports/images/upload-url",
            new CreateReportImageUploadUrlRequest("road.jpg", "image/jpeg", 12_345),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateImageUploadUrl_WithUnsupportedContentType_ShouldReturnBadRequest()
    {
        await AuthenticateAsync("unsupported-image@example.com");

        var response = await _client.PostAsJsonAsync(
            "/api/Reports/images/upload-url",
            new CreateReportImageUploadUrlRequest("road.svg", "image/svg+xml", 12_345),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateImageUploadUrl_WithFileLargerThanFiveMb_ShouldReturnBadRequest()
    {
        await AuthenticateAsync("large-image@example.com");

        var response = await _client.PostAsJsonAsync(
            "/api/Reports/images/upload-url",
            new CreateReportImageUploadUrlRequest("road.jpg", "image/jpeg", (5 * 1024 * 1024) + 1),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateImageUploadUrl_ShouldGenerateUniqueKeysAndIgnoreClientPath()
    {
        await AuthenticateAsync("unique-image@example.com");

        var firstUpload = await CreateImageUploadAsync("../secret/road.jpg");
        var secondUpload = await CreateImageUploadAsync("../secret/road.jpg");

        firstUpload.ImageKey.Should().NotBe(secondUpload.ImageKey);
        firstUpload.ImageKey.Should().NotContain("..");
        firstUpload.ImageKey.Should().NotContain("secret");
        secondUpload.ImageKey.Should().NotContain("..");
        secondUpload.ImageKey.Should().NotContain("secret");
    }

    [Fact]
    public async Task SignedLocalUploadUrl_WithMatchingPutMetadata_ShouldStoreDisplayableImage()
    {
        await AuthenticateAsync("put-image@example.com");
        var upload = await CreateImageUploadAsync(contentLength: 4);
        using var content = new ByteArrayContent([1, 2, 3, 4]);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

        var uploadResponse = await _client.PutAsync(
            new Uri(upload.UploadUrl).PathAndQuery,
            content,
            TestContext.Current.CancellationToken);

        uploadResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var imageResponse = await _client.GetAsync(
            new Uri(upload.ImageUrl).PathAndQuery,
            TestContext.Current.CancellationToken);

        imageResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var storedBytes = await imageResponse.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        storedBytes.Should().Equal([1, 2, 3, 4]);
    }

    [Fact]
    public async Task SignedLocalUploadUrl_PreflightFromFrontendOrigin_ShouldAllowPutUpload()
    {
        await AuthenticateAsync("cors-upload@example.com");
        var upload = await CreateImageUploadAsync();
        using var request = new HttpRequestMessage(
            HttpMethod.Options,
            new Uri(upload.UploadUrl).PathAndQuery);
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "PUT");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.GetValues("Access-Control-Allow-Origin")
            .Should()
            .ContainSingle("http://localhost:3000");
        response.Headers.GetValues("Access-Control-Allow-Methods")
            .Should()
            .Contain(methods => methods.Contains("PUT", StringComparison.OrdinalIgnoreCase));
        response.Headers.GetValues("Access-Control-Allow-Headers")
            .Should()
            .Contain(headers => headers.Contains("content-type", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateReport_WithIssuedImageUrl_ShouldReturnCreatedAndPhotoUrl()
    {
        await AuthenticateAsync("issued-image@example.com");
        var upload = await CreateImageUploadAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/Reports",
            CreateReportRequest(upload.ImageUrl),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var report = await ApiTestClient.ReadDataAsync<ReportResponse>(
            response,
            TestContext.Current.CancellationToken);

        report.PhotoUrl.Should().Be(upload.ImageUrl);
    }

    [Fact]
    public async Task CreateReport_WithUnissuedImageUrl_ShouldReturnBadRequest()
    {
        await AuthenticateAsync("unissued-image@example.com");

        var response = await _client.PostAsJsonAsync(
            "/api/Reports",
            CreateReportRequest("https://example.com/photo.jpg"),
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

        var reports = await ApiTestClient.ReadDataAsync<IReadOnlyList<ReportListItemResponse>>(
            response,
            TestContext.Current.CancellationToken);

        reports.Should().Contain(report => report.Id == createdReport.Id);
    }

    [Fact]
    public async Task GetReportById_ShouldBePublicAndHideCreatorIdentity()
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
    }

    private async Task<ReportResponse> CreateReportAsync()
    {
        var upload = await CreateImageUploadAsync();
        var response = await _client.PostAsJsonAsync(
            "/api/Reports",
            CreateReportRequest(upload.ImageUrl),
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        return await ApiTestClient.ReadDataAsync<ReportResponse>(
            response,
            TestContext.Current.CancellationToken);
    }

    private async Task<ReportImageUploadUrlResponse> CreateImageUploadAsync(
        string fileName = "road.jpg",
        long contentLength = 12_345)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/Reports/images/upload-url",
            new CreateReportImageUploadUrlRequest(fileName, "image/jpeg", contentLength),
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        return await ApiTestClient.ReadDataAsync<ReportImageUploadUrlResponse>(
            response,
            TestContext.Current.CancellationToken);
    }

    private static CreateReportRequest CreateReportRequest(string photoUrl)
    {
        return new CreateReportRequest(
            "A large pothole is damaging vehicles.",
            Category.RoadsId,
            photoUrl,
            "Nairobi",
            "Kenyatta Avenue");
    }
}
