namespace Web.Domain.Entities;

public sealed class ReportConfirmation
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ReportId { get; set; }
    public Report Report { get; set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
