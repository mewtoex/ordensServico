using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Os.Api.Application.Interfaces.Repositories;

namespace Os.Api.Infra.Auth;

public class ActiveUserTokenEvents(IUserRepository users) : JwtBearerEvents
{
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        if (!Guid.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
        {
            context.Fail("Usuário inválido");
            return;
        }
        var user = await users.GetByIdAsync(id);
        if (user is null || !user.Active || user.Role.ToString() != context.Principal?.FindFirstValue(ClaimTypes.Role))
        {
            context.Fail("Acesso revogado");
        }
    }
}
