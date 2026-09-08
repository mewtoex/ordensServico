using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Os.Api.Application;
using Os.Api.Domain;
using Os.Api.Infra;
using PdfSharp.Pdf.IO;
using Xunit;

namespace Os.IntegrationTests;

public class OrderProductTests(SqlServerApiFactory factory) : IClassFixture<SqlServerApiFactory>
{
    [Fact]
    public async Task ReassignmentTransfersAccessAndQuantityEditsKeepHistoricalPrice()
    {
        var (first, firstClient) = await factory.CreateTechnicianAsync();
        var (second, secondClient) = await factory.CreateTechnicianAsync();
        using (firstClient)
        using (secondClient)
        {
            var order = await CreateOrder(first.Id);
            var catalog = await Read<CatalogResponse>(await factory.Admin.PostAsJsonAsync("/api/catalog", new CatalogRequest("Peça", ItemKind.Peca, 25m), SqlServerApiFactory.Json));
            var added = await Read<OrderResponse>(await firstClient.PostAsJsonAsync($"/api/orders/{order.Id}/items", new ItemRequest(catalog.Id, 1)));
            var itemId = added.Items.Single().Id;
            Assert.Equal(HttpStatusCode.OK, (await factory.Admin.PutAsJsonAsync($"/api/catalog/{catalog.Id}", new CatalogRequest("Peça nova", ItemKind.Peca, 99m), SqlServerApiFactory.Json)).StatusCode);
            var changed = await Read<OrderResponse>(await firstClient.PatchAsJsonAsync($"/api/orders/{order.Id}/items/{itemId}/quantity", new UpdateItemQuantityRequest(3)));
            Assert.Equal(75m, changed.Total);
            Assert.Equal(itemId, changed.Items.Single().Id);
            Assert.Equal(25m, changed.Items.Single().UnitPrice);
            Assert.Equal(HttpStatusCode.BadRequest, (await firstClient.PatchAsJsonAsync($"/api/orders/{order.Id}/items/{itemId}/quantity", new UpdateItemQuantityRequest(0))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await firstClient.PatchAsJsonAsync($"/api/orders/{order.Id}/technician", new AssignTechnicianRequest(second.Id))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await factory.Admin.PutAsJsonAsync($"/api/users/{first.Id}", new UpdateUserRequest(first.Name, first.Email, Role.Admin), SqlServerApiFactory.Json)).StatusCode);
            var reassigned = await Read<OrderResponse>(await factory.Admin.PatchAsJsonAsync($"/api/orders/{order.Id}/technician", new AssignTechnicianRequest(second.Id)));
            Assert.Equal(second.Id, reassigned.TechnicianId);
            Assert.Equal(HttpStatusCode.NotFound, (await firstClient.GetAsync($"/api/orders/{order.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await firstClient.GetAsync($"/api/orders/{order.Id}/pdf")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await firstClient.PatchAsJsonAsync($"/api/orders/{order.Id}/items/{itemId}/quantity", new UpdateItemQuantityRequest(2))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await secondClient.GetAsync($"/api/orders/{order.Id}")).StatusCode);
            var pdf = await secondClient.GetAsync($"/api/orders/{order.Id}/pdf");
            pdf.EnsureSuccessStatusCode();
            Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
            Assert.Contains(order.Id.ToString(), pdf.Content.Headers.ContentDisposition!.FileNameStar!);
            using var stream = new MemoryStream(await pdf.Content.ReadAsByteArrayAsync());
            using var document = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
            Assert.True(document.PageCount >= 1);
            Assert.Equal(HttpStatusCode.OK, (await secondClient.PatchAsJsonAsync($"/api/orders/{order.Id}/status", new StatusRequest(OrderStatus.EmAndamento), SqlServerApiFactory.Json)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await secondClient.PatchAsJsonAsync($"/api/orders/{order.Id}/status", new StatusRequest(OrderStatus.Concluida), SqlServerApiFactory.Json)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await secondClient.PatchAsJsonAsync($"/api/orders/{order.Id}/items/{itemId}/quantity", new UpdateItemQuantityRequest(2))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await factory.Admin.PatchAsJsonAsync($"/api/orders/{order.Id}/technician", new AssignTechnicianRequest(first.Id))).StatusCode);
            using var scope = factory.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<OsDb>();
            var persisted = await database.Orders.AsNoTracking().Include(value => value.Items).Include(value => value.History).SingleAsync(value => value.Id == order.Id);
            Assert.Equal(75m, persisted.Total);
            Assert.Single(persisted.History, entry => entry.Action == "QuantidadeAlterada");
            Assert.Single(persisted.History, entry => entry.Action == "TecnicoAlterado");
        }
    }

    [Fact]
    public async Task UserEditionRequiresAdminAndInvalidatesOldIdentity()
    {
        var (user, client) = await factory.CreateTechnicianAsync();
        using (client)
        {
            var request = new UpdateUserRequest("Nome atualizado", "updated-" + user.Email, Role.Tecnico);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/users/{user.Id}", request, SqlServerApiFactory.Json)).StatusCode);
            var changed = await Read<UserResponse>(await factory.Admin.PutAsJsonAsync($"/api/users/{user.Id}", request, SqlServerApiFactory.Json));
            Assert.Equal(request.Email, changed.Email);
            Assert.Equal(request.Name, changed.Name);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
            using var updated = await factory.LoginAsync(changed.Email);
            Assert.Equal(HttpStatusCode.OK, (await updated.GetAsync("/api/auth/me")).StatusCode);
            var admin = await Read<UserResponse>(await factory.Admin.GetAsync("/api/auth/me"));
            Assert.Equal(HttpStatusCode.BadRequest, (await factory.Admin.PutAsJsonAsync($"/api/users/{admin.Id}", new UpdateUserRequest(admin.Name, admin.Email, Role.Tecnico), SqlServerApiFactory.Json)).StatusCode);
        }
    }

    private async Task<OrderResponse> CreateOrder(Guid technicianId)
    {
        var customer = await Read<CustomerResponse>(await factory.Admin.PostAsJsonAsync("/api/customers", new CustomerRequest("José da Conceição", "cliente@integration.example", "11999999999")));
        return await Read<OrderResponse>(await factory.Admin.PostAsJsonAsync("/api/orders", new OrderRequest(customer.Id, technicianId, "Manutenção e substituição de peças.")));
    }
    private static async Task<T> Read<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(SqlServerApiFactory.Json))!;
    }
}
