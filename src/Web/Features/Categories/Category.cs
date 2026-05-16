using Web.Features.Reports;

namespace Web.Features.Categories;

public sealed class Category
{
    public static readonly Guid RoadsId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DrainageId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid StreetLightsId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid BridgesId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string? Description { get; set; }
    public List<Report> Reports { get; } = [];
}
