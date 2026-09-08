using Os.Api.Domain;

namespace Os.Api.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<IReadOnlyList<User>> ListAsync();
    Task<bool> HasAdminAsync();
    Task<bool> IsActiveTechnicianAsync(Guid id);
    void Add(User user);
}
