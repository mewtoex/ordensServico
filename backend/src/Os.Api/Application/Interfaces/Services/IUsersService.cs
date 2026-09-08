namespace Os.Api.Application.Interfaces.Services;

public interface IUsersService
{
    Task<IReadOnlyList<UserResponse>> Users();
    Task<UserResponse> CreateUser(UserRequest request);
    Task<UserResponse> Update(Guid id, UpdateUserRequest request);
    Task Active(Guid id, bool active);
}
