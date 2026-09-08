using Os.Api.Domain;
using Xunit;
namespace Os.Tests;

public class OrderTests
{
    [Fact]
    public void TotalIncludesLaborAndParts()
    {
        var o = new ServiceOrder { Items = [new() { Kind = ItemKind.Servico, Quantity = 1, UnitPrice = 125.50m }, new() { Kind = ItemKind.Peca, Quantity = 3, UnitPrice = 19.90m }] };
        Assert.Equal(185.20m, o.Total);
    }
    [Fact]
    public void LifecycleRecordsActorAndClosure()
    {
        var actor = Guid.NewGuid();
        var o = new ServiceOrder();
        foreach (var s in new[] { OrderStatus.EmAndamento, OrderStatus.AguardandoPeca, OrderStatus.EmAndamento, OrderStatus.Concluida })
            o.ChangeStatus(s, actor);
        Assert.Equal(4, o.History.Count);
        Assert.All(o.History, a => Assert.Equal(actor, a.ActorId));
        Assert.NotNull(o.ClosedAt);
        Assert.Throws<BusinessException>(() => o.EnsureEditable());
        Assert.Throws<BusinessException>(() => o.ChangeStatus(OrderStatus.Cancelada, actor));
    }
    [Theory]
    [InlineData(OrderStatus.AguardandoPeca)]
    [InlineData(OrderStatus.Concluida)]
    [InlineData(OrderStatus.Aberta)]
    [InlineData((OrderStatus)99)]
    public void RejectsInvalidTransitions(OrderStatus s)
    {
        var o = new ServiceOrder();
        Assert.Throws<BusinessException>(() => o.ChangeStatus(s, Guid.NewGuid()));
        Assert.Empty(o.History);
    }
    [Fact]
    public void CancellationIsTerminal()
    {
        var o = new ServiceOrder();
        o.ChangeStatus(OrderStatus.Cancelada, Guid.NewGuid());
        Assert.NotNull(o.ClosedAt);
        Assert.Throws<BusinessException>(() => o.ChangeStatus(OrderStatus.EmAndamento, Guid.NewGuid()));
    }
}
