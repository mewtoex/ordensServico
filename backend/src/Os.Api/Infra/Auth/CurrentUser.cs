using System.Security.Claims;
using Os.Api.Application.Abstractions;
using Os.Api.Domain;

namespace Os.Api.Infra.Auth;

public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid Id => Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id : throw new UnauthorizedAccessException();
    public bool IsAdmin => accessor.HttpContext?.User.IsInRole(nameof(Role.Admin)) == true;
}
