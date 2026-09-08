using Os.Api.Domain;

namespace Os.Api.Application.Abstractions;

public interface ITokenIssuer
{
    LoginResponse Issue(User user);
}
