using Microsoft.EntityFrameworkCore;
using Os.Api.Infra;

namespace Os.IntegrationTests;

public sealed class SynchronizedOsDb : OsDb
{
    private readonly DbContextOptions<OsDb> options;
    private readonly ConcurrentSaveGate gate;

    public SynchronizedOsDb(DbContextOptions<OsDb> options, ConcurrentSaveGate gate) : base(options)
    {
        this.options = options;
        this.gate = gate;
    }

    // EF migrations are attributed to OsDb, not this derived test context.
    public OsDb CreateSchemaContext() => new(options);

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(this, cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }
}
