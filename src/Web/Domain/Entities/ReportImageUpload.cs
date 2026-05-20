namespace Web.Domain.Entities;

public sealed class ReportImageUpload
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string ImageKey { get; init; }
    public required string ImageUrl { get; init; }
    public required string ContentType { get; init; }
    public long ContentLength { get; init; }
    public required string OriginalFileName { get; init; }
    public Guid CreatedByUserId { get; init; }
    public User CreatedByUser { get; init; } = null!;
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAtUtc { get; init; }
    public DateTimeOffset? UsedAtUtc { get; set; }
}
