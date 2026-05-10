using Microsoft.EntityFrameworkCore;
using Web.Infrastructure;
using Web.Infrastructure.Persistence;

namespace Web;

public static class DependencyInjection
{
    public static IServiceCollection AddWebServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var defaultConnection = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(defaultConnection))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");
        }

        services.AddOpenApi();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(defaultConnection));
        services.AddScoped<IDatabaseHealthCheck, DatabaseHealthCheck>();

        return services;
    }
}
