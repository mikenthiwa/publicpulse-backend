using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Web.Features.Auth;
using Web.Features.Reports;
using Web.Infrastructure;
using Web.Infrastructure.Persistence;

namespace Web;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddWebServices(this IHostApplicationBuilder builder)
    {
        var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");

        var jwtOptions = builder.Configuration
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

        builder.Services.AddOpenApi();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddCors(options =>
        {
            var allowedOrigins = builder.Configuration
                .GetSection(CorsOptions.SectionName)
                .Get<CorsOptions>()?
                .AllowedOrigins
                .Where(origin => !string.IsNullOrWhiteSpace(origin))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? [];

            options.AddPolicy(CorsOptions.PolicyName, policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins);
                }
                else
                {
                    policy.AllowAnyOrigin();
                }

                policy
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter a valid JWT bearer token."
            });

            options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer", null, null),
                    []
                }
            });
        });
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
        builder.Services.Configure<ReportImageStorageOptions>(builder.Configuration.GetSection(ReportImageStorageOptions.SectionName));
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
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
            });
        builder.Services.AddAuthorization();
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(defaultConnection));
        builder.Services.AddScoped<IDatabaseHealthCheck, DatabaseHealthCheck>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IReportImageUploadService, ReportImageUploadService>();
        builder.Services.AddScoped<IReportImageStorageService, ReportImageStorageService>();
        builder.Services.AddScoped<IReportService, ReportService>();
        builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

        return builder;
    }
}
