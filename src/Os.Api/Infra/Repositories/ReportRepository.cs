using Microsoft.EntityFrameworkCore;
using Os.Api.Application;
using Os.Api.Application.Interfaces.Repositories;
using Os.Api.Domain;

namespace Os.Api.Infra.Repositories;

public class ReportRepository(OsDb database) : IReportRepository
{
    public async Task<MonthlyReportResponse> GetMonthlyAsync(DateTimeOffset start, DateTimeOffset end)
    {
        var created = await database.Orders.CountAsync(order => order.CreatedAt >= start && order.CreatedAt < end);
        var completedOrders = database.Orders.Where(order => order.Status == OrderStatus.Concluida
            && order.ClosedAt >= start && order.ClosedAt < end);
        var completed = await completedOrders.CountAsync();
        var revenue = await completedOrders.SelectMany(order => order.Items)
            .Where(item => item.DeletedAt == null)
            .SumAsync(item => (decimal?)(item.UnitPrice * item.Quantity)) ?? 0m;
        return new MonthlyReportResponse(start.Year, start.Month, created, completed, revenue);
    }
}
