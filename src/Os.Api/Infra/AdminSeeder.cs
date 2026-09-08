using Microsoft.AspNetCore.Identity;
using Os.Api.Application.Abstractions;
using Os.Api.Application.Interfaces.Repositories;
using Os.Api.Domain;

namespace Os.Api.Infra;

public class AdminSeeder(IUserRepository users, IUnitOfWork unitOfWork, IPasswordHasher<User> passwordHasher, IConfiguration configuration)
{
    public async Task SeedAsync()
    {
        var email = configuration["Seed:Email"]?.Trim().ToLowerInvariant()
            ?? throw new InvalidOperationException("Configure Seed__Email.");
        var password = configuration["Seed:Password"] ?? "";
        if (password.Length < 12)
        {
            throw new InvalidOperationException("Seed__Password precisa de 12 caracteres.");
        }
        if (await users.HasAdminAsync())
        {
            throw new InvalidOperationException("Já existe um administrador.");
        }
        var admin = new User { Name = "Administrador", Email = email, Role = Role.Admin };
        admin.PasswordHash = passwordHasher.HashPassword(admin, password);
        users.Add(admin);
        await unitOfWork.SaveChangesAsync();
    }
}
