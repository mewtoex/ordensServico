using Os.Api.Domain;
using Xunit;

namespace Os.Tests;

public class ProductDomainTests
{
    [Fact]
    public void QuantityUpdatePreservesIdentityAndPriceAndRecordsOnlyRealChanges()
    {
        var actor = Guid.NewGuid();
        var order = new ServiceOrder();
        var catalog = new CatalogItem { Name = "Peça", Price = 30m };
        order.AddItem(catalog, 1, actor);
        var id = order.Items.Single().Id;
        catalog.Price = 99m;
        order.UpdateQuantity(id, 4, actor);
        order.UpdateQuantity(id, 4, actor);
        Assert.Equal(id, order.Items.Single().Id);
        Assert.Equal(120m, order.Total);
        Assert.Equal(2, order.History.Count);
        Assert.Equal("QuantidadeAlterada", order.History.Last().Action);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10001)]
    public void InvalidQuantityLeavesItemAndHistoryUntouched(int quantity)
    {
        var order = new ServiceOrder();
        order.AddItem(new CatalogItem { Price = 5m }, 1, Guid.NewGuid());
        Assert.Throws<BusinessException>(() => order.UpdateQuantity(order.Items.Single().Id, quantity, Guid.NewGuid()));
        Assert.Equal(5m, order.Total);
        Assert.Single(order.History);
    }

    [Fact]
    public void ClosedOrdersRejectReassignmentAndQuantityChanges()
    {
        var order = new ServiceOrder { TechnicianId = Guid.NewGuid() };
        var actor = Guid.NewGuid();
        order.AddItem(new CatalogItem { Price = 5m }, 1, actor);
        order.AssignTechnician(Guid.NewGuid(), actor);
        Assert.Equal("TecnicoAlterado", order.History.Last().Action);
        order.ChangeStatus(OrderStatus.Cancelada, actor);
        Assert.Throws<BusinessException>(() => order.AssignTechnician(Guid.NewGuid(), actor));
        Assert.Throws<BusinessException>(() => order.UpdateQuantity(order.Items.Single().Id, 2, actor));
    }
}
