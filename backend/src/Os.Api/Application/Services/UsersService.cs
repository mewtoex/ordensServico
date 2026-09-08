using Microsoft.AspNetCore.Identity;
using Os.Api.Application.Abstractions;
using Os.Api.Application.Interfaces.Repositories;
using Os.Api.Application.Interfaces.Services;
using Os.Api.Domain;

namespace Os.Api.Application.Services;

public class UsersService(IUserRepository users, IUnitOfWork unitOfWork, IPasswordHasher<User> hasher, ICurrentUser currentUser) : IUsersService
{

    private Guid Actor => currentUser.Id;

    public async Task<IReadOnlyList<UserResponse>> Users() => (await users.ListAsync()).Select(UserResponse.From).ToList();

    public async Task<UserResponse> CreateUser(UserRequest request)
    {
        var user = new User { Name = request.Name.Trim(), Email = request.Email.Trim().ToLowerInvariant(), Role = request.Role };
        user.PasswordHash = hasher.HashPassword(user, request.Password);
        users.Add(user);
        await unitOfWork.SaveChangesAsync();
        return UserResponse.From(user);
    }

    public async Task<UserResponse> Update(Guid id, UpdateUserRequest request)
    {
        var user = await users.GetByIdAsync(id) ?? throw new KeyNotFoundException();
        if (user.Id == Actor && request.Role != Role.Admin)
        {
            throw new BusinessException("Não é permitido remover o próprio perfil de administrador.");
        }
        if (user.Role == Role.Tecnico && request.Role != Role.Tecnico && await users.HasOpenOrdersAsync(id))
        {
            throw new BusinessException("Reatribua as OS abertas antes de alterar o perfil deste técnico.");
        }
        var email = request.Email.Trim().ToLowerInvariant();
        if (email != user.Email || request.Role != user.Role)
        {
            user.SecurityVersion = Guid.NewGuid();
        }
        user.Name = request.Name.Trim();
        user.Email = email;
        user.Role = request.Role;
        await unitOfWork.SaveChangesAsync();
        return UserResponse.From(user);
    }

    public async Task Active(Guid id, bool active)
    {
        if (id == Actor)
            throw new BusinessException("Não é permitido alterar a ativação do próprio usuário.");
        var user = await users.GetByIdAsync(id) ?? throw new KeyNotFoundException();
        if (user.Active != active)
        {
            user.SecurityVersion = Guid.NewGuid();
        }
        user.Active = active;
        await unitOfWork.SaveChangesAsync();
    }
}
