using Os.Api.Domain;

namespace Os.Api.Application.Interfaces.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id);
    Task<PagedResponse<Customer>> ListAsync(string? search, int page, int pageSize, Guid? technicianId);
    void Add(Customer customer);
    void Remove(Customer customer);
}
