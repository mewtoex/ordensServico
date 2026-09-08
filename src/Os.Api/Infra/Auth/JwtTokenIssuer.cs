using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Os.Api.Application;
using Os.Api.Application.Abstractions;
using Os.Api.Domain;

namespace Os.Api.Infra.Auth;

public class JwtTokenIssuer(IConfiguration configuration) : ITokenIssuer
{
    public const string Issuer = "os-api";
    public const string Audience = "os-client";
    public LoginResponse Issue(User user)
    {
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        var token = new JwtSecurityToken(Issuer, Audience, claims, expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expiresAt, UserResponse.From(user));
    }
}
