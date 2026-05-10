namespace Web.Infrastructure.Persistence;

public sealed class DatabaseHealthCheck(ApplicationDbContext dbContext) : IDatabaseHealthCheck
{
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        return dbContext.Database.CanConnectAsync(cancellationToken);
    }
}
