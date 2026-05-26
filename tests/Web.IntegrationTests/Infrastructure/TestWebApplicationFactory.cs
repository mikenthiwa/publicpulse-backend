using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Web.Features.Reports;
using Web.Infrastructure.Persistence;

namespace Web.IntegrationTests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"publicpulse-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IReportImageCloudinaryService>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
            services.AddScoped<IReportImageCloudinaryService, FakeReportImageCloudinaryService>();
            services.AddTransient<IStartupFilter, TestDatabaseStartupFilter>();
        });
    }

    private sealed class FakeReportImageCloudinaryService : IReportImageCloudinaryService
    {
        public ReportImageUploadSignatureResponse CreateUploadSignature(Guid userId)
        {
            return new ReportImageUploadSignatureResponse(
                "public-pulse",
                "test-api-key",
                1_800_000_000,
                GetUserFolder(userId),
                "test-upload-preset",
                "test-upload-signature");
        }

        public bool IsUploadResultValid(CreateReportImageRequest image)
        {
            return image.Signature == "valid-signature";
        }

        public string GetUserFolder(Guid userId)
        {
            return $"public-pulse/reports/{userId:N}";
        }

        public string CreateImageUrl(string publicId, string version)
        {
            var escapedPublicId = string.Join(
                "/",
                publicId
                    .Trim()
                    .Split('/', StringSplitOptions.RemoveEmptyEntries)
                    .Select(Uri.EscapeDataString));

            return $"https://res.cloudinary.com/public-pulse/image/upload/v{version.Trim()}/{escapedPublicId}";
        }
    }
}
