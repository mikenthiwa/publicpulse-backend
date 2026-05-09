namespace Web.Infrastructure;

public static class EndpointRouteBuilderExtensions
{
    public static WebApplication MapEndpointGroups(this WebApplication app)
    {
        var endpointGroupType = typeof(EndpointGroupBase);
        var assembly = typeof(EndpointGroupBase).Assembly;

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
