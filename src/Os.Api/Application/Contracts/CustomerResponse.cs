using Os.Api.Domain;

namespace Os.Api.Application;

public record CustomerResponse(Guid Id, string Name, string Email, string Phone)
{
    public static CustomerResponse From(Customer customer) => new(customer.Id, customer.Name, customer.Email, customer.Phone);
}
