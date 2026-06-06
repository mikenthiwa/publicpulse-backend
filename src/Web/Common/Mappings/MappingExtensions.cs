using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Web.Common.Models;
using Web.Features.Auth;

namespace Web.Common.Mappings;

public static class MappingExtensions
{
    public static void AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>();

        if (jwtOptions is null
            || string.IsNullOrWhiteSpace(jwtOptions.Issuer)
            || string.IsNullOrWhiteSpace(jwtOptions.Audience)
            || string.IsNullOrWhiteSpace(jwtOptions.SigningKey)
            || jwtOptions.ExpiryMinutes <= 0)
        {
            throw new InvalidOperationException("JWT configuration is required.");
        }
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        var problemDetails = new ProblemDetails()
                        {
                            Status = StatusCodes.Status401Unauthorized,
                            Title = "Unauthorized.",
                            Detail = "Authentication is required to access this resource.",
                            Instance = context.Request.Path,
                            Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
                        };

                        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

                        return context.Response.WriteAsJsonAsync(
                            problemDetails,
                            (System.Text.Json.JsonSerializerOptions?)null,
                            "application/problem+json",
                            CancellationToken.None);
                    },
                    OnForbidden = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        var problemDetails = new ProblemDetails
                        {
                            Status = StatusCodes.Status403Forbidden,
                            Title = "Forbidden.",
                            Detail = "You are not allowed to access this resource.",
                            Instance = context.Request.Path,
                            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3"
                        };

                        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

                        return context.Response.WriteAsJsonAsync(
                            problemDetails,
                            (System.Text.Json.JsonSerializerOptions?)null,
                            "application/problem+json",
                            CancellationToken.None);
                    }
                };
        });
    }

    public static Task<PaginatedList<T>> PaginateAsync<T>(
        this IQueryable<T> source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return PaginatedList<T>.CreateAsync(source.AsNoTracking(), pageNumber, pageSize, cancellationToken);
    }
}
