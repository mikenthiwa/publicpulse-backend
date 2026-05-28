namespace Web.Features.Locations;

public sealed class MapboxOptions
{
    public const string SectionName = "Mapbox";

    public string? AccessToken { get; set; }

    public string BaseUrl { get; set; } = "https://api.mapbox.com/";
}
