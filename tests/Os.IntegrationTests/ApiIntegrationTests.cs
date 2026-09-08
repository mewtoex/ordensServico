using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Os.Api.Application;
using Os.Api.Domain;
using Os.Api.Infra;
using Xunit;

namespace Os.IntegrationTests;

public class ApiIntegrationTests(SqlServerApiFactory factory) : IClassFixture<SqlServerApiFactory>
{
    [Fact]
    public async Task FullLifecyclePersistsTotalsHistoryAndMonthlyRevenue()
    {
        var (technician, client) = await factory.CreateTechnicianAsync();
        using (client)
        {
            var order = await CreateOrderAsync(technician.Id);
            var labor = await CreateCatalogAsync(ItemKind.Servico, 125.50m);
            var part = await CreateCatalogAsync(ItemKind.Peca, 19.90m);
            await AssertStatusAsync(await client.PostAsJsonAsync($"/api/orders/{order.Id}/items", new ItemRequest(labor.Id, 1), SqlServerApiFactory.Json), HttpStatusCode.OK);
            var added = await client.PostAsJsonAsync($"/api/orders/{order.Id}/items", new ItemRequest(part.Id, 3), SqlServerApiFactory.Json);
            await AssertStatusAsync(added, HttpStatusCode.OK);
            Assert.Equal(185.20m, (await ReadAsync<OrderResponse>(added)).Total);

            await AssertStatusAsync(await factory.Admin.PutAsJsonAsync($"/api/catalog/{part.Id}",
                new CatalogRequest("Preço alterado", ItemKind.Peca, 99m), SqlServerApiFactory.Json), HttpStatusCode.OK);
            await AssertStatusAsync(await client.PatchAsJsonAsync($"/api/orders/{order.Id}/status", new StatusRequest(OrderStatus.EmAndamento), SqlServerApiFactory.Json), HttpStatusCode.OK);
            await AssertStatusAsync(await client.PatchAsJsonAsync($"/api/orders/{order.Id}/status", new StatusRequest(OrderStatus.AguardandoPeca), SqlServerApiFactory.Json), HttpStatusCode.OK);
            await AssertStatusAsync(await client.PatchAsJsonAsync($"/api/orders/{order.Id}/status", new StatusRequest(OrderStatus.EmAndamento), SqlServerApiFactory.Json), HttpStatusCode.OK);
            var completed = await client.PatchAsJsonAsync($"/api/orders/{order.Id}/status", new StatusRequest(OrderStatus.Concluida), SqlServerApiFactory.Json);
            await AssertStatusAsync(completed, HttpStatusCode.OK);
            Assert.NotNull((await ReadAsync<OrderResponse>(completed)).ClosedAt);
            await AssertStatusAsync(await client.PostAsJsonAsync($"/api/orders/{order.Id}/items", new ItemRequest(part.Id, 1), SqlServerApiFactory.Json), HttpStatusCode.BadRequest);
            await AssertStatusAsync(await client.PatchAsJsonAsync($"/api/orders/{order.Id}/status", new StatusRequest(OrderStatus.Cancelada), SqlServerApiFactory.Json), HttpStatusCode.BadRequest);

            using var scope = factory.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<OsDb>();
            var persisted = await database.Orders.AsNoTracking().Include(value => value.Items).Include(value => value.History).SingleAsync(value => value.Id == order.Id);
            Assert.Equal(OrderStatus.Concluida, persisted.Status);
            Assert.Equal(185.20m, persisted.Total);
            Assert.Equal(7, persisted.History.Count);
            Assert.All(persisted.History.Where(entry => entry.Action != "Criacao"), entry => Assert.Equal(technician.Id, entry.ActorId));
            Assert.NotEmpty(persisted.Version);
            var now = DateTimeOffset.UtcNow;
            var report = await ReadAsync<MonthlyReportResponse>(await factory.Admin.GetAsync($"/api/reports/monthly?year={now.Year}&month={now.Month}"));
            Assert.True(report.Revenue >= 185.20m);
            var history = await ReadAsync<List<AuditResponse>>(await client.GetAsync($"/api/orders/{order.Id}/history"));
            Assert.Equal(7, history.Count);
        }
    }

    [Fact]
    public async Task PermissionsIsolateTechniciansAndRejectRevokedOrInvalidTokens()
    {
        var (owner, ownerClient) = await factory.CreateTechnicianAsync();
        var (other, otherClient) = await factory.CreateTechnicianAsync();
        using (ownerClient)
        using (otherClient)
        using (var anonymous = factory.CreateClient())
        {
            var order = await CreateOrderAsync(owner.Id);
            var item = await CreateCatalogAsync(ItemKind.Peca, 10m);
            await AssertStatusAsync(await anonymous.GetAsync("/api/orders"), HttpStatusCode.Unauthorized);
            await AssertStatusAsync(await ownerClient.GetAsync("/api/users"), HttpStatusCode.Forbidden);
            await AssertStatusAsync(await ownerClient.GetAsync("/api/reports/monthly?year=2026&month=9"), HttpStatusCode.Forbidden);
            await AssertStatusAsync(await ownerClient.PostAsJsonAsync("/api/orders", new OrderRequest(order.CustomerId, owner.Id, "Sem permissão")), HttpStatusCode.Forbidden);
            await AssertStatusAsync(await otherClient.GetAsync($"/api/orders/{order.Id}"), HttpStatusCode.NotFound);
            await AssertStatusAsync(await otherClient.GetAsync($"/api/orders/{order.Id}/history"), HttpStatusCode.NotFound);
            await AssertStatusAsync(await otherClient.GetAsync($"/api/orders/{order.Id}/summary"), HttpStatusCode.NotFound);
            await AssertStatusAsync(await otherClient.PostAsJsonAsync($"/api/orders/{order.Id}/items", new ItemRequest(item.Id, 1)), HttpStatusCode.NotFound);
            await AssertStatusAsync(await otherClient.PatchAsJsonAsync($"/api/orders/{order.Id}/status", new StatusRequest(OrderStatus.EmAndamento), SqlServerApiFactory.Json), HttpStatusCode.NotFound);
            var filtered = await ReadAsync<PagedResponse<OrderResponse>>(await otherClient.GetAsync($"/api/orders?customerId={order.CustomerId}"));
            Assert.Empty(filtered.Data);
            await AssertStatusAsync(await factory.Admin.PatchAsync($"/api/users/{other.Id}/active?active=false", null), HttpStatusCode.NoContent);
            await AssertStatusAsync(await otherClient.GetAsync("/api/auth/me"), HttpStatusCode.Unauthorized);
            await AssertStatusAsync(await anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest(SqlServerApiFactory.AdminEmail, "incorrect")), HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task ConcurrentStatusUpdatesReturnConflictAndRollbackLosingAudit()
    {
        var (technician, client) = await factory.CreateTechnicianAsync();
        using (client)
        {
            var order = await CreateOrderAsync(technician.Id);
            await AssertStatusAsync(await client.PatchAsJsonAsync($"/api/orders/{order.Id}/status", new StatusRequest(OrderStatus.EmAndamento), SqlServerApiFactory.Json), HttpStatusCode.OK);
            factory.SaveGate.Arm(order.Id);
            var responses = await Task.WhenAll(
                client.PatchAsJsonAsync($"/api/orders/{order.Id}/status", new StatusRequest(OrderStatus.Concluida), SqlServerApiFactory.Json),
                client.PatchAsJsonAsync($"/api/orders/{order.Id}/status", new StatusRequest(OrderStatus.Cancelada), SqlServerApiFactory.Json));
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
            var conflict = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
            Assert.Equal("application/problem+json", conflict.Content.Headers.ContentType?.MediaType);
            var winningOrder = await ReadAsync<OrderResponse>(responses.Single(response => response.IsSuccessStatusCode));
            using var scope = factory.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<OsDb>();
            var persisted = await database.Orders.AsNoTracking().Include(value => value.History).SingleAsync(value => value.Id == order.Id);
            Assert.Equal(winningOrder.Status, persisted.Status);
            Assert.Equal(3, persisted.History.Count);
            Assert.Single(persisted.History, entry => entry.Detail.EndsWith("-> " + winningOrder.Status));
        }
    }

    [Fact]
    public async Task ConcurrentItemAdditionsKeepOnlyWinningItemAndAudit()
    {
        var (technician, client) = await factory.CreateTechnicianAsync();
        using (client)
        {
            var order = await CreateOrderAsync(technician.Id);
            var item = await CreateCatalogAsync(ItemKind.Peca, 20m);
            factory.SaveGate.Arm(order.Id);
            var responses = await Task.WhenAll(
                client.PostAsJsonAsync($"/api/orders/{order.Id}/items", new ItemRequest(item.Id, 1)),
                client.PostAsJsonAsync($"/api/orders/{order.Id}/items", new ItemRequest(item.Id, 2)));
            var success = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
            var winningOrder = await ReadAsync<OrderResponse>(success);
            using var scope = factory.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<OsDb>();
            var persisted = await database.Orders.AsNoTracking().Include(value => value.Items)
                .Include(value => value.History).SingleAsync(value => value.Id == order.Id);
            Assert.Single(persisted.Items);
            Assert.Equal(winningOrder.Total, persisted.Total);
            Assert.Equal(2, persisted.History.Count);
            Assert.Single(persisted.History, entry => entry.Action == "ItemAdicionado");
        }
    }

    [Fact]
    public async Task CorsAllowsConfiguredPreflightAndRejectsUnknownOrigin()
    {
        using var client = factory.CreateClient();
        using var allowed = Preflight(SqlServerApiFactory.AllowedOrigin);
        var response = await client.SendAsync(allowed);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(SqlServerApiFactory.AllowedOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("PATCH", string.Join(",", response.Headers.GetValues("Access-Control-Allow-Methods")));
        using var denied = Preflight("https://untrusted.example");
        Assert.False((await client.SendAsync(denied)).Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task SwaggerDeclaresBearerOnlyForProtectedEndpoints()
    {
        using var client = factory.CreateClient();
        using var json = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var root = json.RootElement;
        Assert.Equal("bearer", root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer").GetProperty("scheme").GetString());
        var paths = root.GetProperty("paths");
        Assert.True(paths.GetProperty("/api/orders").GetProperty("get").GetProperty("security")[0].TryGetProperty("Bearer", out _));
        var login = paths.GetProperty("/api/auth/login").GetProperty("post");
        Assert.False(login.TryGetProperty("security", out var security) && security.GetArrayLength() > 0);
    }

    [Fact]
    public async Task HealthChecksDatabaseAndKeepsLivenessIndependent()
    {
        using var client = factory.CreateClient();
        await AssertStatusAsync(await client.GetAsync("/health"), HttpStatusCode.OK);
        await using var unavailable = new SqlServerApiFactory("Server=127.0.0.1,1;User Id=unavailable;Password=not-a-real-secret;TrustServerCertificate=true;Connect Timeout=1");
        using var disconnected = unavailable.CreateClient();
        var unhealthy = await disconnected.GetAsync("/health");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unhealthy.StatusCode);
        Assert.DoesNotContain("not-a-real-secret", await unhealthy.Content.ReadAsStringAsync());
        await AssertStatusAsync(await disconnected.GetAsync("/health/live"), HttpStatusCode.OK);
    }

    private async Task<OrderResponse> CreateOrderAsync(Guid technicianId)
    {
        var customer = await ReadAsync<CustomerResponse>(await factory.Admin.PostAsJsonAsync("/api/customers", new CustomerRequest("Cliente integração", "customer@integration.example", "11999999999")));
        return await ReadAsync<OrderResponse>(await factory.Admin.PostAsJsonAsync("/api/orders", new OrderRequest(customer.Id, technicianId, "Ordem de integração")));
    }

    private async Task<CatalogResponse> CreateCatalogAsync(ItemKind kind, decimal price) => await ReadAsync<CatalogResponse>(
        await factory.Admin.PostAsJsonAsync("/api/catalog", new CatalogRequest("Item integração", kind, price), SqlServerApiFactory.Json));

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(SqlServerApiFactory.Json))!;
    }

    private static async Task AssertStatusAsync(HttpResponseMessage response, HttpStatusCode status) =>
        Assert.True(response.StatusCode == status, $"Expected {status}, got {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

    private static HttpRequestMessage Preflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/orders");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "PATCH");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
        return request;
    }
}
