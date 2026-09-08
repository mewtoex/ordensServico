using Microsoft.EntityFrameworkCore;
using Os.Api.Application;
using Os.Api.Application.Interfaces.Repositories;
using Os.Api.Domain;

namespace Os.Api.Infra.Repositories;

public class UserRepository(OsDb database) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id) => database.Users.SingleOrDefaultAsync(user => user.Id == id);
    public Task<User?> GetByEmailAsync(string email) => database.Users.SingleOrDefaultAsync(user => user.Email == email);
    public async Task<IReadOnlyList<User>> ListAsync() => await database.Users.AsNoTracking().OrderBy(user => user.Name).ThenBy(user => user.Id).ToListAsync();
    public Task<bool> HasAdminAsync() => database.Users.AnyAsync(user => user.Role == Role.Admin);
    public Task<bool> IsActiveTechnicianAsync(Guid id) => database.Users.AnyAsync(user => user.Id == id && user.Active && user.Role == Role.Tecnico);
    public Task<bool> HasOpenOrdersAsync(Guid id) => database.Orders.AnyAsync(order => order.TechnicianId == id
        && order.Status != OrderStatus.Concluida && order.Status != OrderStatus.Cancelada);
    public void Add(User user) => database.Users.Add(user);
}
