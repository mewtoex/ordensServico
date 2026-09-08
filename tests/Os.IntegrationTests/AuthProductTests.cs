using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Os.Api.Application;
using Os.Api.Infra;
using Xunit;

namespace Os.IntegrationTests;

public class AuthProductTests(SqlServerApiFactory factory) : IClassFixture<SqlServerApiFactory>
{
    [Fact]
    public async Task PasswordChangeRevokesAccessAndRefreshWhileRotationIsSingleUse()
    {
        var (user, originalClient) = await factory.CreateTechnicianAsync();
        using (originalClient)
        using (var anonymous = factory.CreateClient())
        {
            var login = await Login(anonymous, user.Email, SqlServerApiFactory.Password);
            Assert.NotNull(login.RefreshToken);
            using (var scope = factory.Services.CreateScope())
            {
                var database = scope.ServiceProvider.GetRequiredService<OsDb>();
                var hash = Hash(login.RefreshToken!);
                var stored = await database.RefreshSessions.AsNoTracking().SingleAsync(session => session.TokenHash == hash);
                Assert.NotEqual(login.RefreshToken, stored.TokenHash);
            }
            var renewed = await Refresh(anonymous, login.RefreshToken!);
            Assert.NotEqual(login.RefreshToken, renewed.RefreshToken);
            Assert.NotEqual(login.AccessToken, renewed.AccessToken);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(login.RefreshToken!))).StatusCode);
            using var authenticated = factory.CreateClient();
            authenticated.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", renewed.AccessToken);
            Assert.Equal(HttpStatusCode.BadRequest, (await authenticated.PostAsJsonAsync("/api/auth/change-password", new ChangePasswordRequest("wrong", "New-Password-456!"))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await authenticated.GetAsync("/api/auth/me")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await authenticated.PostAsJsonAsync("/api/auth/change-password", new ChangePasswordRequest(SqlServerApiFactory.Password, "New-Password-456!"))).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await authenticated.GetAsync("/api/auth/me")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await originalClient.GetAsync("/api/auth/me")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(renewed.RefreshToken!))).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest(user.Email, SqlServerApiFactory.Password))).StatusCode);
            Assert.NotNull((await Login(anonymous, user.Email, "New-Password-456!")).RefreshToken);
        }
    }

    [Fact]
    public async Task ExpirationConcurrentRotationAndReactivationDoNotResurrectOldSessions()
    {
        var (user, client) = await factory.CreateTechnicianAsync();
        using (client)
        using (var anonymous = factory.CreateClient())
        {
            var expired = await Login(anonymous, user.Email, SqlServerApiFactory.Password);
            using (var scope = factory.Services.CreateScope())
            {
                var database = scope.ServiceProvider.GetRequiredService<OsDb>();
                var hash = Hash(expired.RefreshToken!);
                var session = await database.RefreshSessions.SingleAsync(session => session.TokenHash == hash);
                session.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1);
                await database.SaveChangesAsync();
            }
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(expired.RefreshToken!))).StatusCode);
            var login = await Login(anonymous, user.Email, SqlServerApiFactory.Password);
            int before;
            using (var scope = factory.Services.CreateScope())
            {
                var database = scope.ServiceProvider.GetRequiredService<OsDb>();
                var hash = Hash(login.RefreshToken!);
                var session = await database.RefreshSessions.AsNoTracking().SingleAsync(session => session.TokenHash == hash);
                before = await database.RefreshSessions.CountAsync(session => session.UserId == user.Id);
                factory.SaveGate.ArmSession(session.Id);
            }
            var responses = await Task.WhenAll(
                anonymous.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(login.RefreshToken!)),
                anonymous.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(login.RefreshToken!)));
            Assert.Equal(2, factory.SaveGate.Arrivals);
            var success = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
            var renewed = (await success.Content.ReadFromJsonAsync<LoginResponse>(SqlServerApiFactory.Json))!;
            using (var scope = factory.Services.CreateScope())
            {
                var database = scope.ServiceProvider.GetRequiredService<OsDb>();
                Assert.Equal(before + 1, await database.RefreshSessions.CountAsync(session => session.UserId == user.Id));
            }
            Assert.Equal(HttpStatusCode.NoContent, (await factory.Admin.PatchAsync($"/api/users/{user.Id}/active?active=false", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(renewed.RefreshToken!))).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await factory.Admin.PatchAsync($"/api/users/{user.Id}/active?active=true", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(renewed.RefreshToken!))).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        }
    }

    [Fact]
    public async Task LoginRateLimitStillApplies()
    {
        await using var limited = factory.WithWebHostBuilder(builder => builder.UseSetting("RateLimiting:LoginPermitLimit", "1"));
        using var client = limited.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("invalid@example.com", "invalid"))).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("invalid@example.com", "invalid"))).StatusCode);
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static async Task<LoginResponse> Login(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>(SqlServerApiFactory.Json))!;
    }
    private static async Task<LoginResponse> Refresh(HttpClient client, string token)
    {
        var response = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(token));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>(SqlServerApiFactory.Json))!;
    }
}
