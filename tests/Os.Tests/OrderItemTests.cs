using Os.Api.Domain;
using Xunit;

namespace Os.Tests;

public class OrderItemTests
{
    [Fact]
    public void CatalogChangesDoNotChangeExistingOrderItems()
    {
        var order = new ServiceOrder();
        var catalogItem = new CatalogItem { Name = "Diagnóstico", Kind = ItemKind.Servico, Price = 80m };
        var actorId = Guid.NewGuid();

        order.AddItem(catalogItem, 2, actorId);
        catalogItem.Price = 120m;
        catalogItem.Name = "Diagnóstico completo";

        Assert.Equal(160m, order.Total);
        Assert.Equal("Diagnóstico", Assert.Single(order.Items).Name);
        Assert.Equal(actorId, Assert.Single(order.History).ActorId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10001)]
    public void InvalidQuantityDoesNotChangeOrder(int quantity)
    {
        var order = new ServiceOrder();

        Assert.Throws<BusinessException>(() => order.AddItem(new CatalogItem(), quantity, Guid.NewGuid()));
        Assert.Empty(order.Items);
        Assert.Empty(order.History);
    }

    [Fact]
    public void RemovingItemRecalculatesTotalAndRecordsActor()
    {
        var order = new ServiceOrder();
        var actorId = Guid.NewGuid();
        order.AddItem(new CatalogItem { Name = "Peça", Price = 35m }, 3, actorId);

        order.RemoveItem(Assert.Single(order.Items).Id, actorId);

        Assert.Equal(0m, order.Total);
        Assert.Equal("ItemRemovido", order.History.Last().Action);
        Assert.Equal(actorId, order.History.Last().ActorId);
    }

    [Fact]
    public void CancelledOrderRejectsItemChanges()
    {
        var order = new ServiceOrder();
        var actorId = Guid.NewGuid();
        order.AddItem(new CatalogItem { Price = 10m }, 1, actorId);
        order.ChangeStatus(OrderStatus.Cancelada, actorId);

        Assert.Throws<BusinessException>(() => order.AddItem(new CatalogItem(), 1, actorId));
        Assert.Throws<BusinessException>(() => order.RemoveItem(order.Items.Single().Id, actorId));
        Assert.Equal(10m, order.Total);
        Assert.Equal(2, order.History.Count);
    }
}
