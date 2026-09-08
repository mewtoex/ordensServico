using Os.Api.Domain;

namespace Os.Api.Application;

public record UserResponse(Guid Id, string Name, string Email, Role Role, bool Active)
{
    public static UserResponse From(User user) => new(user.Id, user.Name, user.Email, user.Role, user.Active);
}
