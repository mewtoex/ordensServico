using Microsoft.EntityFrameworkCore;
using Os.Api.Application;
using Os.Api.Application.Interfaces.Repositories;
using Os.Api.Domain;

namespace Os.Api.Infra.Repositories;

public class CustomerRepository(OsDb database) : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(Guid id) => database.Customers.SingleOrDefaultAsync(customer => customer.Id == id);
    public async Task<PagedResponse<Customer>> ListAsync(string? search, int page, int pageSize, Guid? technicianId)
    {
        var query = database.Customers.AsNoTracking();
        if (technicianId.HasValue)
        {
            query = query.Where(customer => database.Orders.Any(order => order.CustomerId == customer.Id && order.TechnicianId == technicianId));
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(customer => customer.Name.Contains(search));
        }
        var total = await query.CountAsync();
        var customers = await query.OrderBy(customer => customer.Name)
            .ThenBy(customer => customer.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return new PagedResponse<Customer>(total, page, pageSize, customers);
    }
    public void Add(Customer customer) => database.Customers.Add(customer);
    public void Remove(Customer customer) => database.Customers.Remove(customer);
}
