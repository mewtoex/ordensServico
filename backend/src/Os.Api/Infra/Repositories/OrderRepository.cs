using Microsoft.EntityFrameworkCore;
using Os.Api.Application;
using Os.Api.Application.Interfaces.Repositories;
using Os.Api.Domain;

namespace Os.Api.Infra.Repositories;

public class OrderRepository(OsDb database) : IOrderRepository
{
    public Task<ServiceOrder?> GetByIdAsync(Guid id, Guid? technicianId) => VisibleOrders(technicianId)
        .Include(order => order.Items)
        .Include(order => order.Customer)
        .Include(order => order.History)
        .SingleOrDefaultAsync(order => order.Id == id);

    public async Task<PagedResponse<ServiceOrder>> ListAsync(OrderFilter filter, Guid? technicianId)
    {
        var query = VisibleOrders(technicianId).AsNoTracking();
        if (filter.CustomerId.HasValue)
        {
            query = query.Where(order => order.CustomerId == filter.CustomerId);
        }
        if (filter.Status.HasValue)
        {
            query = query.Where(order => order.Status == filter.Status);
        }
        if (filter.From.HasValue)
        {
            query = query.Where(order => order.CreatedAt >= filter.From);
        }
        if (filter.To.HasValue)
        {
            query = query.Where(order => order.CreatedAt < filter.To);
        }
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(order => order.Customer.Name.Contains(filter.Search));
        }
        var total = await query.CountAsync();
        var orders = await query.Include(order => order.Items)
            .Include(order => order.Customer)
            .OrderByDescending(order => order.CreatedAt)
            .ThenBy(order => order.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();
        return new PagedResponse<ServiceOrder>(total, filter.Page, filter.PageSize, orders);
    }

    public async Task<IReadOnlyList<AuditResponse>> GetHistoryAsync(Guid id) => await database.AuditEntries
        .AsNoTracking()
        .Where(entry => entry.ServiceOrderId == id)
        .OrderBy(entry => entry.At)
        .ThenBy(entry => entry.Id)
        .Select(entry => new AuditResponse(entry.Id, entry.ActorId, entry.Actor.Name, entry.Action, entry.Detail, entry.At))
        .ToListAsync();

    public void Add(ServiceOrder order) => database.Orders.Add(order);

    public void MarkChanged(ServiceOrder order)
    {
        // Force an UPDATE on the aggregate root so rowversion also protects item mutations.
        database.Entry(order).Property(current => current.Description).IsModified = true;
    }

    private IQueryable<ServiceOrder> VisibleOrders(Guid? technicianId) => database.Orders
        .Where(order => !technicianId.HasValue || order.TechnicianId == technicianId);
}
