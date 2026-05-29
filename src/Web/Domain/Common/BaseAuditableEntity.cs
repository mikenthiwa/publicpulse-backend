namespace Web.Domain.Common;

public abstract class BaseAuditableEntity
{
    public DateTimeOffset Created { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastModified { get; set; }
    public Guid? CreatedBy { get; set; }
}
