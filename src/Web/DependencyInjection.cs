using System.Diagnostics;
using System.Reflection;
using System.Text;
using CloudinaryDotNet;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;
using Web.Common.Factory;
using Web.Domain.Entities;
using Web.Features.Auth;
using Web.Features.Auth.Login;
using Web.Features.Auth.Register;
using Web.Features.Categories.ListCategories;
using Web.Features.Locations;
using Web.Features.Reports;
using Web.Features.Reports.ConfirmReport;
using Web.Features.Reports.CreateImageUploadSignature;
using Web.Features.Reports.CreateReport;
using Web.Features.Reports.GetReportById;
using Web.Features.Reports.ListReport;
using Web.Features.Reports.UpdateReportStatus;
using Web.Infrastructure;
using Web.Infrastructure.Identity;
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
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, CurrentUser>();
        builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
        builder.Services.Configure<CloudinaryOptions>(builder.Configuration.GetSection(CloudinaryOptions.SectionName));
        builder.Services.Configure<MapboxOptions>(builder.Configuration.GetSection(MapboxOptions.SectionName));
        builder.Services.PostConfigure<MapboxOptions>(options =>
        {
            options.AccessToken = string.IsNullOrWhiteSpace(options.AccessToken)
                ? builder.Configuration["MAPBOX_ACCESS_TOKEN"]
                : options.AccessToken;
        });
        builder.Services.AddSingleton(serviceProvider =>
        {
            var cloudinaryOptions = serviceProvider.GetRequiredService<IOptions<CloudinaryOptions>>().Value;

            if (string.IsNullOrWhiteSpace(cloudinaryOptions.CloudName)
                || string.IsNullOrWhiteSpace(cloudinaryOptions.ApiKey)
                || string.IsNullOrWhiteSpace(cloudinaryOptions.ApiSecret)
                || string.IsNullOrWhiteSpace(cloudinaryOptions.UploadPreset))
            {
                throw new ReportImageUploadException("Cloudinary configuration is missing.");
            }

            return new Cloudinary(new Account(
                cloudinaryOptions.CloudName,
                cloudinaryOptions.ApiKey,
                cloudinaryOptions.ApiSecret));
        });
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

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        var problemDetails = new ProblemDetails
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
        builder.Services.AddAuthorization();
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(defaultConnection));
        builder.Services.AddScoped<IDatabaseHealthCheck, DatabaseHealthCheck>();
        builder.Services.AddHttpClient<IReverseGeocodingProvider, MapboxReverseGeocodingProvider>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        builder.Services.AddScoped<IReportImageCloudinaryService, CloudinaryReportImageService>();
        builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
        builder.Services.AddScoped<LoginHandler>();
        builder.Services.AddScoped<RegisterHandler>();
        builder.Services.AddScoped<ListCategoriesHandler>();
        builder.Services.AddScoped<ReverseGeocodeHandler>();
        builder.Services.AddScoped<CreateImageUploadSignatureHandler>();
        builder.Services.AddScoped<CreateReportHandler>();
        builder.Services.AddScoped<ListReportHandler>();
        builder.Services.AddScoped<GetReportByIdHandler>();
        builder.Services.AddScoped<ConfirmReportHandler>();
        builder.Services.AddScoped<UpdateReportStatusHandler>();
        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        builder.Services.AddFluentValidationAutoValidation(configuration =>
        {
            configuration.OverrideDefaultResultFactoryWith<CustomResultFactory>();
        });
        
        return builder;
    }
}
