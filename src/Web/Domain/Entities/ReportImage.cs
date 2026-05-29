using Web.Domain.Common;

namespace Web.Domain.Entities;

public sealed class ReportImage : BaseAuditableEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ReportId { get; set; }
    public Report Report { get; set; } = null!;
    public required string ImageUrl { get; init; }
    public required string PublicId { get; init; }
}
