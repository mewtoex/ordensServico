using Os.Api.Application.Abstractions;
using Os.Api.Application.Interfaces.Repositories;
using Os.Api.Application.Interfaces.Services;
using Os.Api.Domain;

namespace Os.Api.Application.Services;

public class CustomersService(ICustomerRepository customers, IUnitOfWork unitOfWork, ICurrentUser currentUser) : ICustomersService
{

    private Guid Actor => currentUser.Id;
    private bool Admin => currentUser.IsAdmin;

    public async Task<PagedResponse<CustomerResponse>> Customers(string? search, int page = 1, int pageSize = 20)
    {
        Pagination.Validate(page, pageSize);
        var result = await customers.ListAsync(search, page, pageSize, Admin ? null : Actor);
        return new PagedResponse<CustomerResponse>(result.Total, result.Page, result.PageSize,
            result.Data.Select(CustomerResponse.From).ToList());
    }

    public async Task<CustomerResponse> CreateCustomer(CustomerRequest request)
    {
        var customer = new Customer { Name = request.Name.Trim(), Email = request.Email.Trim().ToLowerInvariant(), Phone = request.Phone.Trim() };
        customers.Add(customer);
        await unitOfWork.SaveChangesAsync();
        return CustomerResponse.From(customer);
    }

    public async Task<CustomerResponse> EditCustomer(Guid id, CustomerRequest request)
    {
        var customer = await customers.GetByIdAsync(id) ?? throw new KeyNotFoundException();
        customer.Name = request.Name.Trim();
        customer.Email = request.Email.Trim().ToLowerInvariant();
        customer.Phone = request.Phone.Trim();
        await unitOfWork.SaveChangesAsync();
        return CustomerResponse.From(customer);
    }

    public async Task DeleteCustomer(Guid id)
    {
        customers.SoftDelete(await customers.GetByIdAsync(id) ?? throw new KeyNotFoundException());
        await unitOfWork.SaveChangesAsync();
    }
}
