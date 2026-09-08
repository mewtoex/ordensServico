using Microsoft.EntityFrameworkCore;
using Os.Api.Application;
using Os.Api.Domain;
using Os.Api.Infra;
using Os.Api.Infra.Repositories;
using Xunit;

namespace Os.Tests;

// InMemory verifies query behavior, not SQL Server constraints, transactions or rowversion.
public class RepositoryTests
{
    [Fact]
    public async Task OrdersFilterByTechnicianCustomerStatusAndHalfOpenPeriodBeforePagination()
    {
        await using var database = CreateDatabase();
        var technicianId = Guid.NewGuid();
        var customer = new Customer { Name = "Ana" };
        var start = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        var first = NewOrder(customer, technicianId, start);
        var second = NewOrder(customer, technicianId, start.AddDays(1));
        var third = NewOrder(customer, technicianId, start.AddDays(2));
        var foreign = NewOrder(customer, Guid.NewGuid(), start.AddDays(1));
        var outsidePeriod = NewOrder(customer, technicianId, start.AddMonths(1));
        var cancelled = NewOrder(customer, technicianId, start.AddDays(1));
        cancelled.ChangeStatus(OrderStatus.Cancelada, technicianId);
        database.Orders.AddRange(first, second, third, foreign, outsidePeriod, cancelled);
        await database.SaveChangesAsync();
        var repository = new OrderRepository(database);

        var result = await repository.ListAsync(new OrderFilter
        {
            CustomerId = customer.Id,
            Search = "Ana",
            Status = OrderStatus.Aberta,
            From = start,
            To = start.AddMonths(1),
            Page = 2,
            PageSize = 1
        }, technicianId);

        Assert.Equal(3, result.Total);
        Assert.Equal(second.Id, Assert.Single(result.Data).Id);
        Assert.Null(await repository.GetByIdAsync(foreign.Id, technicianId));
        Assert.NotNull(await repository.GetByIdAsync(foreign.Id, null));
    }

    [Fact]
    public async Task CustomersAreLimitedToTechniciansOrders()
    {
        await using var database = CreateDatabase();
        var technicianId = Guid.NewGuid();
        var ownCustomer = new Customer { Name = "Ana" };
        database.Orders.AddRange(
            NewOrder(ownCustomer, technicianId, DateTimeOffset.UtcNow),
            NewOrder(new Customer { Name = "Bruno" }, Guid.NewGuid(), DateTimeOffset.UtcNow));
        await database.SaveChangesAsync();
        var repository = new CustomerRepository(database);

        var technicianResult = await repository.ListAsync(null, 1, 20, technicianId);
        var adminResult = await repository.ListAsync(null, 1, 20, null);

        Assert.Equal(ownCustomer.Id, Assert.Single(technicianResult.Data).Id);
        Assert.Equal(2, adminResult.Total);
    }

    [Fact]
    public async Task MonthlyRevenueIncludesOnlyCompletedOrdersInPeriod()
    {
        await using var database = CreateDatabase();
        var technicianId = Guid.NewGuid();
        var start = new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var completed = NewOrder(new Customer(), technicianId, start);
        completed.AddItem(new CatalogItem { Price = 25m }, 2, technicianId);
        completed.ChangeStatus(OrderStatus.EmAndamento, technicianId);
        completed.ChangeStatus(OrderStatus.Concluida, technicianId);
        var open = NewOrder(new Customer(), technicianId, start);
        open.AddItem(new CatalogItem { Price = 999m }, 1, technicianId);
        database.Orders.AddRange(completed, open);
        await database.SaveChangesAsync();
        var repository = new ReportRepository(database);

        var result = await repository.GetMonthlyAsync(start, start.AddMonths(1));
        var empty = await repository.GetMonthlyAsync(start.AddMonths(2), start.AddMonths(3));

        Assert.Equal(2, result.Created);
        Assert.Equal(1, result.Completed);
        Assert.Equal(50m, result.Revenue);
        Assert.Equal(0m, empty.Revenue);
    }

    private static OsDb CreateDatabase() => new(new DbContextOptionsBuilder<OsDb>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static ServiceOrder NewOrder(Customer customer, Guid technicianId, DateTimeOffset createdAt) => new()
    {
        Customer = customer,
        CustomerId = customer.Id,
        TechnicianId = technicianId,
        CreatedAt = createdAt
    };
}
