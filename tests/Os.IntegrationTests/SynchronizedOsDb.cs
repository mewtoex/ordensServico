using Microsoft.EntityFrameworkCore;
using Os.Api.Infra;

namespace Os.IntegrationTests;

public sealed class SynchronizedOsDb(DbContextOptions<OsDb> options, ConcurrentSaveGate gate) : OsDb(options)
{
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(this, cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }
}
