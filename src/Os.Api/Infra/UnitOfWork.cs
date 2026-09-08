using Os.Api.Application.Abstractions;

namespace Os.Api.Infra;

public class UnitOfWork(OsDb database) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => database.SaveChangesAsync(cancellationToken);
}
