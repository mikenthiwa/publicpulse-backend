using Web.Domain.Common;

namespace Web.Domain.Entities;

public sealed class ReportConfirmation : BaseAuditableEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ReportId { get; set; }
    public Report Report { get; set; } = null!;
}
