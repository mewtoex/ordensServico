using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Os.Api.Application.Abstractions;
using Os.Api.Application.Interfaces.Repositories;
using Os.Api.Application.Interfaces.Services;
using Os.Api.Domain;

namespace Os.Api.Application.Services;

public class AuthService(IUserRepository users, IPasswordHasher<User> hasher, ICurrentUser currentUser,
    ITokenIssuer tokenIssuer, IRefreshSessionRepository sessions, IUnitOfWork unitOfWork) : IAuthService
{
    public async Task<LoginResponse?> Login(LoginRequest request)
    {
        var user = await users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is null || !user.Active || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return null;
        }
        return await IssueSessionAsync(user);
    }

    public async Task<LoginResponse?> Refresh(RefreshRequest request)
    {
        var session = await sessions.GetByHashAsync(Hash(request.RefreshToken));
        if (session is null || session.ConsumedAt.HasValue || session.ExpiresAt <= DateTimeOffset.UtcNow
            || !session.User.Active || session.SecurityVersion != session.User.SecurityVersion)
        {
            return null;
        }
        session.ConsumedAt = DateTimeOffset.UtcNow;
        // Rotation and insertion of the replacement are committed in the same transaction.
        return await IssueSessionAsync(session.User);
    }

    public async Task ChangePassword(ChangePasswordRequest request)
    {
        var user = await users.GetByIdAsync(currentUser.Id) ?? throw new KeyNotFoundException();
        if (hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            throw new BusinessException("Senha atual incorreta.");
        }
        if (request.CurrentPassword == request.NewPassword)
        {
            throw new BusinessException("A nova senha deve ser diferente da senha atual.");
        }
        user.PasswordHash = hasher.HashPassword(user, request.NewPassword);
        user.SecurityVersion = Guid.NewGuid();
        await unitOfWork.SaveChangesAsync();
    }

    public async Task<UserResponse> Me() => UserResponse.From(await users.GetByIdAsync(currentUser.Id) ?? throw new KeyNotFoundException());

    private async Task<LoginResponse> IssueSessionAsync(User user)
    {
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);
        sessions.Add(new RefreshSession
        {
            UserId = user.Id,
            TokenHash = Hash(rawToken),
            SecurityVersion = user.SecurityVersion,
            ExpiresAt = expiresAt
        });
        await unitOfWork.SaveChangesAsync();
        return tokenIssuer.Issue(user) with { RefreshToken = rawToken, RefreshExpiresAt = expiresAt };
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
