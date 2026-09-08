using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Os.Api.Domain;

namespace Os.IntegrationTests;

// Synchronizes two HTTP requests after both have read the same SQL Server rowversion.
public sealed class ConcurrentSaveGate : SaveChangesInterceptor
{
    private Guid _orderId;
    private int _arrivals;
    private TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Arm(Guid orderId)
    {
        _orderId = orderId;
        _arrivals = 0;
        _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (_orderId != Guid.Empty && eventData.Context!.ChangeTracker.Entries<ServiceOrder>()
            .Any(entry => entry.Entity.Id == _orderId && entry.State == EntityState.Modified))
        {
            if (Interlocked.Increment(ref _arrivals) == 2)
            {
                _orderId = Guid.Empty;
                _release.TrySetResult();
            }
            await _release.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
        }
        return result;
    }
}
