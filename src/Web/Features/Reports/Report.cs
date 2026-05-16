using Web.Features.Auth;
using Web.Features.Categories;

namespace Web.Features.Reports;

public sealed class Report
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Title { get; set; }
    public required string Description { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public required string PhotoUrl { get; set; }
    public required string County { get; set; }
    public required string RoadName { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Reported;
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public List<ReportConfirmation> Confirmations { get; } = [];
}
