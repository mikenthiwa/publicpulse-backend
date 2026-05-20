using Web.Domain.Common;

namespace Web.Domain.Entities;

public sealed class User : BaseAuditableEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
}
