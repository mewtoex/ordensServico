namespace Os.Api.Application.Interfaces.Services;

public interface IAuthService
{
    Task<LoginResponse?> Login(LoginRequest request);
    Task<UserResponse> Me();
    Task<LoginResponse?> Refresh(RefreshRequest request);
    Task ChangePassword(ChangePasswordRequest request);
    Task Logout();
}
