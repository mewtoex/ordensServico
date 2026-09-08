using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Os.Api.Application;
using Os.Api.Domain;
using Os.Api.Infra;
using Os.Api.Infra.Repositories;
using Xunit;

namespace Os.IntegrationTests;

public class SoftDeletionTests(SqlServerApiFactory factory) : IClassFixture<SqlServerApiFactory>
{
    [Fact]
    public async Task DeletionPreservesRelationshipsAndExcludesRemovedItemsFromRevenue()
    {
        var (technician, client) = await factory.CreateTechnicianAsync();
        using (client)
        {
            var customer = await Read<CustomerResponse>(await factory.Admin.PostAsJsonAsync("/api/customers", new CustomerRequest("Cliente arquivado", "archived@example.com", "11999999999")));
            var catalog = await Read<CatalogResponse>(await factory.Admin.PostAsJsonAsync("/api/catalog", new CatalogRequest("Peça arquivada", ItemKind.Peca, 25m), SqlServerApiFactory.Json));
            var order = await Read<OrderResponse>(await factory.Admin.PostAsJsonAsync("/api/orders", new OrderRequest(customer.Id, technician.Id, "Exclusão lógica")));
            var added = await Read<OrderResponse>(await client.PostAsJsonAsync($"/api/orders/{order.Id}/items", new ItemRequest(catalog.Id, 2)));
            var itemId = added.Items.Single().Id;
            Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"/api/customers/{customer.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"/api/catalog/{catalog.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await factory.Admin.DeleteAsync($"/api/customers/{customer.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await factory.Admin.DeleteAsync($"/api/catalog/{catalog.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await factory.Admin.DeleteAsync($"/api/catalog/{catalog.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await factory.Admin.PostAsJsonAsync("/api/orders", new OrderRequest(customer.Id, technician.Id, "Nova OS"))).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/orders/{order.Id}/items", new ItemRequest(catalog.Id, 1))).StatusCode);
            var preserved = await Read<OrderResponse>(await client.GetAsync($"/api/orders/{order.Id}"));
            Assert.Equal(customer.Name, preserved.CustomerName);
            Assert.Equal(50m, preserved.Total);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/orders/{order.Id}/pdf")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/orders/{order.Id}/items/{itemId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/orders/{order.Id}/items/{itemId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsJsonAsync($"/api/orders/{order.Id}/items/{itemId}/quantity", new UpdateItemQuantityRequest(3))).StatusCode);
            var removed = await Read<OrderResponse>(await client.GetAsync($"/api/orders/{order.Id}"));
            Assert.Empty(removed.Items);
            Assert.Equal(0m, removed.Total);
            (await client.PatchAsJsonAsync($"/api/orders/{order.Id}/status", new StatusRequest(OrderStatus.EmAndamento), SqlServerApiFactory.Json)).EnsureSuccessStatusCode();
            (await client.PatchAsJsonAsync($"/api/orders/{order.Id}/status", new StatusRequest(OrderStatus.Concluida), SqlServerApiFactory.Json)).EnsureSuccessStatusCode();

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OsDb>();
            Assert.NotNull((await db.Customers.SingleAsync(value => value.Id == customer.Id)).DeletedAt);
            Assert.NotNull((await db.Catalog.SingleAsync(value => value.Id == catalog.Id)).DeletedAt);
            Assert.NotNull((await db.Set<OrderItem>().SingleAsync(value => value.Id == itemId)).DeletedAt);
            Assert.Single(await db.AuditEntries.Where(value => value.ServiceOrderId == order.Id && value.Action == "ItemRemovido").ToListAsync());
            var customers = new CustomerRepository(db);
            Assert.Null(await customers.GetByIdAsync(customer.Id));
            Assert.Empty((await customers.ListAsync(customer.Name, 1, 20, null)).Data);
            var items = new CatalogRepository(db);
            Assert.Null(await items.GetByIdAsync(catalog.Id));
            Assert.DoesNotContain((await items.ListAsync(1, 100)).Data, value => value.Id == catalog.Id);
            var now = DateTimeOffset.UtcNow;
            var start = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
            Assert.Equal(0m, (await new ReportRepository(db).GetMonthlyAsync(start, start.AddMonths(1))).Revenue);
        }
    }

    private static async Task<T> Read<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(SqlServerApiFactory.Json))!;
    }
}
