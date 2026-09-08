using Microsoft.EntityFrameworkCore;
using Os.Api.Domain;
using Os.Api.Infra;

namespace Os.IntegrationTests;

// Test-only barrier: two distinct contexts must arrive before either can save.
public sealed class ConcurrentSaveGate
{
    private readonly object sync = new();
    private Guid orderId;
    private Guid sessionId;
    private readonly HashSet<Guid> arrivals = [];
    private TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int Arrivals { get { lock (sync) return arrivals.Count; } }
    public void Arm(Guid id) => Arm(id, Guid.Empty);
    public void ArmSession(Guid id) => Arm(Guid.Empty, id);
    private void Arm(Guid order, Guid session)
    {
        lock (sync)
        {
            orderId = order;
            sessionId = session;
            arrivals.Clear();
            release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
    public async Task WaitAsync(OsDb database, CancellationToken cancellationToken)
    {
        Task wait;
        lock (sync)
        {
            database.ChangeTracker.DetectChanges();
            var matches = (orderId != Guid.Empty && database.ChangeTracker.Entries<ServiceOrder>()
                .Any(entry => entry.Entity.Id == orderId && entry.State == EntityState.Modified))
                || (sessionId != Guid.Empty && database.ChangeTracker.Entries<RefreshSession>()
                .Any(entry => entry.Entity.Id == sessionId && entry.State == EntityState.Modified));
            if (!matches)
                return;
            arrivals.Add(database.ContextId.InstanceId);
            wait = release.Task;
            if (arrivals.Count == 2)
            {
                orderId = sessionId = Guid.Empty;
                release.TrySetResult();
            }
        }
        await wait.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
    }
}
