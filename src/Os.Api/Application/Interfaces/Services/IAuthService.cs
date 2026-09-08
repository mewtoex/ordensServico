namespace Os.Api.Application.Interfaces.Services;

public interface IAuthService
{
    Task<LoginResponse?> Login(LoginRequest request);
    Task<UserResponse> Me();
}
