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
    [HttpPost("refresh"), AllowAnonymous, EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponse>> Refresh(RefreshRequest request)
    {
        var result = await service.Refresh(request);
        return result is null ? Unauthorized() : Ok(result);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        await service.ChangePassword(request);
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me() => Ok(await service.Me());
}
