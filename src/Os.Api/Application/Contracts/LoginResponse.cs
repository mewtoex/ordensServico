using Os.Api.Domain;

namespace Os.Api.Application;

public record LoginResponse(string AccessToken, DateTime ExpiresAt, UserResponse User);
