using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Web.Infrastructure;

public sealed class ApiVersionDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var paths = new OpenApiPaths();

        foreach (var (path, item) in swaggerDoc.Paths)
        {
            paths.Add(path.Replace("{version}", "1", StringComparison.Ordinal), item);
        }

        swaggerDoc.Paths = paths;
    }
}
