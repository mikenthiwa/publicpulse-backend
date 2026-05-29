using System.Reflection;

namespace Web.Infrastructure;

public static class WebApplicationExtensions
{
    public static RouteGroupBuilder MapGroup(this WebApplication app, EndpointGroupBase group)
    {
        var groupName = group.GetType().Name;

        return app
            .MapGroup($"/api/{groupName}")
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
