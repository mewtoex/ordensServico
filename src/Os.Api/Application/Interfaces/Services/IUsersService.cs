namespace Os.Api.Application.Interfaces.Services;

public interface IUsersService
{
    Task<IReadOnlyList<UserResponse>> Users();
    Task<UserResponse> CreateUser(UserRequest request);
    Task Active(Guid id, bool active);
}
