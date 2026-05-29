namespace Web.Features.Locations;

public static class LocationConfidence
{
    public const string High = "high";
    public const string Medium = "medium";
    public const string Low = "low";
}

public sealed record LocationLookupResponse(
    string? County,
    string? RoadName,
    string? LocationLabel,
    double Latitude,
    double Longitude,
    string Source,
    string Confidence);
