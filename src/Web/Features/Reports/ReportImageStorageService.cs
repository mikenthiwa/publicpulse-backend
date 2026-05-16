using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Web.Features.Auth;

namespace Web.Features.Reports;

public interface IReportImageStorageService
{
    ReportImageUploadTarget CreateUploadTarget(
        string imageKey,
        string contentType,
        long contentLength,
        DateTimeOffset expiresAtUtc);

    Task UploadLocalAsync(
        string token,
        HttpRequest request,
        CancellationToken cancellationToken);
}

public sealed record ReportImageUploadTarget(
    string UploadUrl,
    string ImageUrl,
    IReadOnlyDictionary<string, string> Headers);

public sealed class ReportImageStorageService(
    IOptions<ReportImageStorageOptions> options,
    IConfiguration configuration,
    IWebHostEnvironment environment) : IReportImageStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ReportImageStorageOptions _options = options.Value;

    public ReportImageUploadTarget CreateUploadTarget(
        string imageKey,
        string contentType,
        long contentLength,
        DateTimeOffset expiresAtUtc)
    {
        if (string.Equals(_options.Provider, "S3", StringComparison.OrdinalIgnoreCase))
        {
            return CreateS3UploadTarget(imageKey, contentType, expiresAtUtc);
        }

        return CreateLocalUploadTarget(imageKey, contentType, contentLength, expiresAtUtc);
    }

    public async Task UploadLocalAsync(
        string token,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        var payload = ValidateLocalUploadToken(token);

        if (payload.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("Upload URL has expired.");
        }

        if (!HttpMethods.IsPut(request.Method))
        {
            throw new InvalidOperationException("Upload must use PUT.");
        }

        if (!string.Equals(request.ContentType, payload.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Upload content type does not match the signed metadata.");
        }

        if (request.ContentLength != payload.ContentLength)
        {
            throw new InvalidOperationException("Upload content length does not match the signed metadata.");
        }

        var storageRoot = GetLocalStorageRoot();
        var fullPath = Path.GetFullPath(Path.Combine(storageRoot, payload.ImageKey));

        if (!fullPath.StartsWith(storageRoot, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Image key is invalid.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var output = File.Create(fullPath);
        await request.Body.CopyToAsync(output, cancellationToken);
    }

    private ReportImageUploadTarget CreateLocalUploadTarget(
        string imageKey,
        string contentType,
        long contentLength,
        DateTimeOffset expiresAtUtc)
    {
        var payload = new LocalUploadTokenPayload(imageKey, contentType, contentLength, expiresAtUtc);
        var token = CreateLocalUploadToken(payload);
        var uploadBaseUrl = GetRequiredBaseUrl(_options.UploadBaseUrl, "ReportImages:UploadBaseUrl");
        var publicBaseUrl = GetRequiredBaseUrl(_options.PublicBaseUrl, "ReportImages:PublicBaseUrl");

        return new ReportImageUploadTarget(
            $"{uploadBaseUrl}/api/Reports/images/uploads/{UrlEncoder.Default.Encode(token)}",
            $"{publicBaseUrl}/uploads/{imageKey}",
            new Dictionary<string, string>
            {
                ["Content-Type"] = contentType
            });
    }

    private ReportImageUploadTarget CreateS3UploadTarget(
        string imageKey,
        string contentType,
        DateTimeOffset expiresAtUtc)
    {
        var endpoint = RequireOption(_options.S3Endpoint, "ReportImages:S3Endpoint").TrimEnd('/');
        var region = RequireOption(_options.S3Region, "ReportImages:S3Region");
        var bucket = RequireOption(_options.S3Bucket, "ReportImages:S3Bucket");
        var accessKeyId = RequireOption(_options.S3AccessKeyId, "ReportImages:S3AccessKeyId");
        var secretAccessKey = RequireOption(_options.S3SecretAccessKey, "ReportImages:S3SecretAccessKey");
        var publicBaseUrl = GetRequiredBaseUrl(_options.PublicBaseUrl, "ReportImages:PublicBaseUrl");
        var now = DateTimeOffset.UtcNow;
        var amzDate = now.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var dateStamp = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var expiresSeconds = Math.Max(1, (int)(expiresAtUtc - now).TotalSeconds);
        var host = new Uri(endpoint).Host;
        var canonicalUri = _options.S3UsePathStyle
            ? $"/{Uri.EscapeDataString(bucket)}/{EscapeS3Key(imageKey)}"
            : $"/{EscapeS3Key(imageKey)}";
        var uploadHost = _options.S3UsePathStyle
            ? host
            : $"{bucket}.{host}";
        var algorithm = "AWS4-HMAC-SHA256";
        var credentialScope = $"{dateStamp}/{region}/s3/aws4_request";
        var credential = $"{accessKeyId}/{credentialScope}";
        var signedHeaders = "content-type;host";
        var query = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["X-Amz-Algorithm"] = algorithm,
            ["X-Amz-Credential"] = credential,
            ["X-Amz-Date"] = amzDate,
            ["X-Amz-Expires"] = expiresSeconds.ToString(CultureInfo.InvariantCulture),
            ["X-Amz-SignedHeaders"] = signedHeaders
        };
        var canonicalQueryString = string.Join(
            "&",
            query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        var canonicalHeaders = $"content-type:{contentType}\nhost:{uploadHost}\n";
        var canonicalRequest = string.Join(
            "\n",
            "PUT",
            canonicalUri,
            canonicalQueryString,
            canonicalHeaders,
            signedHeaders,
            "UNSIGNED-PAYLOAD");
        var stringToSign = string.Join(
            "\n",
            algorithm,
            amzDate,
            credentialScope,
            ToHex(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest))));
        var signingKey = GetSignatureKey(secretAccessKey, dateStamp, region, "s3");
        var signature = ToHex(HmacSha256(signingKey, stringToSign));
        var uploadUrl = _options.S3UsePathStyle
            ? $"{endpoint}{canonicalUri}?{canonicalQueryString}&X-Amz-Signature={signature}"
            : $"{new Uri(endpoint).Scheme}://{uploadHost}{canonicalUri}?{canonicalQueryString}&X-Amz-Signature={signature}";

        return new ReportImageUploadTarget(
            uploadUrl,
            $"{publicBaseUrl}/{imageKey}",
            new Dictionary<string, string>
            {
                ["Content-Type"] = contentType
            });
    }

    private string CreateLocalUploadToken(LocalUploadTokenPayload payload)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        var body = WebEncoders.Base64UrlEncode(json);
        var signature = WebEncoders.Base64UrlEncode(HmacSha256(GetLocalSigningKey(), body));

        return $"{body}.{signature}";
    }

    private LocalUploadTokenPayload ValidateLocalUploadToken(string token)
    {
        var tokenParts = token.Split('.', 2);

        if (tokenParts.Length != 2)
        {
            throw new InvalidOperationException("Upload token is invalid.");
        }

        var expectedSignature = WebEncoders.Base64UrlEncode(HmacSha256(GetLocalSigningKey(), tokenParts[0]));

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(expectedSignature),
                Encoding.ASCII.GetBytes(tokenParts[1])))
        {
            throw new InvalidOperationException("Upload token is invalid.");
        }

        var payloadBytes = WebEncoders.Base64UrlDecode(tokenParts[0]);
        return JsonSerializer.Deserialize<LocalUploadTokenPayload>(payloadBytes, JsonOptions)
            ?? throw new InvalidOperationException("Upload token is invalid.");
    }

    private byte[] GetLocalSigningKey()
    {
        var signingKey = _options.SigningKey
            ?? configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()?.SigningKey;

        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException("Report image upload signing key is required.");
        }

        return Encoding.UTF8.GetBytes(signingKey);
    }

    private string GetLocalStorageRoot()
    {
        var configuredPath = _options.LocalStoragePath;
        var storagePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(environment.ContentRootPath, "uploads")
            : configuredPath;

        return Path.GetFullPath(storagePath);
    }

    private string GetRequiredBaseUrl(string? value, string optionName)
    {
        return RequireOption(value, optionName).TrimEnd('/');
    }

    private static string RequireOption(string? value, string optionName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{optionName} is required.");
        }

        return value;
    }

    private static string EscapeS3Key(string imageKey)
    {
        return string.Join("/", imageKey.Split('/').Select(Uri.EscapeDataString));
    }

    private static byte[] GetSignatureKey(string key, string dateStamp, string regionName, string serviceName)
    {
        var kDate = HmacSha256(Encoding.UTF8.GetBytes($"AWS4{key}"), dateStamp);
        var kRegion = HmacSha256(kDate, regionName);
        var kService = HmacSha256(kRegion, serviceName);
        return HmacSha256(kService, "aws4_request");
    }

    private static byte[] HmacSha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static string ToHex(byte[] bytes)
    {
        return Convert.ToHexStringLower(bytes);
    }

    private sealed record LocalUploadTokenPayload(
        string ImageKey,
        string ContentType,
        long ContentLength,
        DateTimeOffset ExpiresAtUtc);
}
