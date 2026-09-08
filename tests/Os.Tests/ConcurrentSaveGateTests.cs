using Microsoft.EntityFrameworkCore;
using Os.Api.Domain;
using Os.Api.Infra;
using Os.IntegrationTests;
using Xunit;

namespace Os.Tests;

public class ConcurrentSaveGateTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BarrierRequiresTwoDistinctContexts(bool refresh)
    {
        var gate = new ConcurrentSaveGate();
        var options = new DbContextOptionsBuilder<OsDb>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var first = new SynchronizedOsDb(options, gate);
        await using var second = new SynchronizedOsDb(options, gate);
        var id = Guid.NewGuid();
        if (refresh)
        {
            var a = new RefreshSession { Id = id };
            var b = new RefreshSession { Id = id };
            first.Attach(a);
            second.Attach(b);
            a.ConsumedAt = b.ConsumedAt = DateTimeOffset.UtcNow;
            gate.ArmSession(id);
        }
        else
        {
            var a = new ServiceOrder { Id = id };
            var b = new ServiceOrder { Id = id };
            first.Attach(a);
            second.Attach(b);
            a.ChangeStatus(OrderStatus.EmAndamento, Guid.NewGuid());
            b.ChangeStatus(OrderStatus.Cancelada, Guid.NewGuid());
            gate.Arm(id);
        }
        var waiting = gate.WaitAsync(first, CancellationToken.None);
        Assert.False(waiting.IsCompleted);
        var repeated = gate.WaitAsync(first, CancellationToken.None);
        Assert.False(repeated.IsCompleted);
        Assert.Equal(1, gate.Arrivals);
        await gate.WaitAsync(second, CancellationToken.None);
        await Task.WhenAll(waiting, repeated);
        Assert.Equal(2, gate.Arrivals);
    }

    [Fact]
    public async Task TestContextBlocksPersistenceUntilSecondArrival()
    {
        var gate = new ConcurrentSaveGate();
        var options = new DbContextOptionsBuilder<OsDb>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using (var seed = new OsDb(options))
        {
            seed.Orders.Add(new ServiceOrder());
            await seed.SaveChangesAsync();
        }
        await using var first = new SynchronizedOsDb(options, gate);
        await using var second = new SynchronizedOsDb(options, gate);
        var a = await first.Orders.SingleAsync();
        var b = await second.Orders.SingleAsync();
        a.ChangeStatus(OrderStatus.EmAndamento, Guid.NewGuid());
        b.ChangeStatus(OrderStatus.Cancelada, Guid.NewGuid());
        gate.Arm(a.Id);
        var saving = first.SaveChangesAsync();
        Assert.False(saving.IsCompleted);
        await second.SaveChangesAsync();
        await saving;
        Assert.Equal(2, gate.Arrivals);
    }
}
