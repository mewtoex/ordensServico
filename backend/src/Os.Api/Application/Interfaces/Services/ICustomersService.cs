namespace Os.Api.Application.Interfaces.Services;

public interface ICustomersService
{
    Task<PagedResponse<CustomerResponse>> Customers(string? search, int page = 1, int pageSize = 20);
    Task<CustomerResponse> CreateCustomer(CustomerRequest request);
    Task<CustomerResponse> EditCustomer(Guid id, CustomerRequest request);
    Task DeleteCustomer(Guid id);
}
