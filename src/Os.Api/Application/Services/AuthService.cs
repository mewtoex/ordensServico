using Microsoft.AspNetCore.Identity;
using Os.Api.Application.Abstractions;
using Os.Api.Application.Interfaces.Repositories;
using Os.Api.Application.Interfaces.Services;
using Os.Api.Domain;

namespace Os.Api.Application.Services;

public class AuthService(IUserRepository users, IPasswordHasher<User> hasher, ICurrentUser currentUser, ITokenIssuer tokenIssuer) : IAuthService
{

    private Guid Actor => currentUser.Id;

    public async Task<LoginResponse?> Login(LoginRequest request)
    {
        var user = await users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is null || !user.Active || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            return null;
        return tokenIssuer.Issue(user);
    }

    public async Task<UserResponse> Me() => UserResponse.From(await users.GetByIdAsync(Actor) ?? throw new KeyNotFoundException());
}
