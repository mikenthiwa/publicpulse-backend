using Web.Domain.Common;
using Web.Domain.Enums;

namespace Web.Domain.Entities;

public sealed class Report : BaseAuditableEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Description { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public required string County { get; set; }
    public required string RoadName { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Reported;
    public User CreatedByUser { get; set; } = null!;
    public List<ReportConfirmation> Confirmations { get; } = [];
    public List<ReportImage> Images { get; } = [];
}
