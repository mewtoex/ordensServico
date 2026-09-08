using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Os.Api.Application;
using Os.Api.Application.Interfaces.Services;
using Os.Api.Domain;

namespace Os.Api.Controllers;

[ApiController, Route("api/auth"), Authorize]
public class AuthController(IAuthService service) : ControllerBase
{
    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var result = await service.Login(request);
        return result is null ? Unauthorized() : Ok(result);
    }
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me() => Ok(await service.Me());
}
