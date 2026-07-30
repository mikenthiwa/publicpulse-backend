using System.Reflection;
using Asp.Versioning.Builder;

namespace Web.Infrastructure;

public static class WebApplicationExtensions
{
    public static RouteGroupBuilder MapGroup(this WebApplication app, EndpointGroupBase endpointGroup)
    {
        var groupName = endpointGroup.GetType().Name;

        return app.NewVersionedApi(groupName)
            .MapGroup($"/api/v{{version:apiVersion}}/{groupName}")
            .HasApiVersion(1.0)
            .WithTags(groupName);
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        var endpointGroupType = typeof(EndpointGroupBase);
        var assembly = Assembly.GetExecutingAssembly();

        var endpointGroups = assembly.GetExportedTypes()
            .Where(type => type.IsSubclassOf(endpointGroupType))
            .Select(Activator.CreateInstance)
            .Cast<EndpointGroupBase>();

        foreach (var group in endpointGroups)
        {
            group.Map(app);
        }

        return app;
    }
}
