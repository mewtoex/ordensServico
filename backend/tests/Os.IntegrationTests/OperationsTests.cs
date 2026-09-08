using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Os.Api.Application;
using Os.Api.Infra;
using Os.DatabaseTools;
using Xunit;

namespace Os.IntegrationTests;

public class OperationsTests(SqlServerApiFactory factory) : IClassFixture<SqlServerApiFactory>
{
    [Fact]
    public async Task LogoutRevokesAllOwnSessionsWithoutAffectingAnotherUser()
    {
        var (user, client) = await factory.CreateTechnicianAsync();
        using (client)
        using (var anonymous = factory.CreateClient())
        {
            var first = await Login(anonymous, user.Email);
            var second = await Login(anonymous, user.Email);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsync("/api/auth/logout", null)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
            foreach (var session in new[] { first, second })
            {
                Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(session.RefreshToken!))).StatusCode);
                using var authorized = factory.CreateClient();
                authorized.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
                Assert.Equal(HttpStatusCode.Unauthorized, (await authorized.GetAsync("/api/auth/me")).StatusCode);
            }
            Assert.Equal(HttpStatusCode.OK, (await factory.Admin.GetAsync("/api/auth/me")).StatusCode);
            using var fresh = await factory.LoginAsync(user.Email);
            Assert.Equal(HttpStatusCode.OK, (await fresh.GetAsync("/api/auth/me")).StatusCode);
        }
    }

    [Fact]
    public async Task MetricsRequireAdminAndRequestsExposeTraceIdentifier()
    {
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/metrics")).StatusCode);
        var (_, technician) = await factory.CreateTechnicianAsync();
        using (technician)
            Assert.Equal(HttpStatusCode.Forbidden, (await technician.GetAsync("/metrics")).StatusCode);
        var missing = await factory.Admin.GetAsync($"/api/orders/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var traceId = Assert.Single(missing.Headers.GetValues("X-Trace-Id"));
        var problem = await missing.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(traceId, problem.GetProperty("traceId").GetString());
        var response = await factory.Admin.GetAsync("/metrics");
        response.EnsureSuccessStatusCode();
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        var metrics = await response.Content.ReadAsStringAsync();
        Assert.Contains("os_http_requests_total{status_class=\"4xx\"}", metrics);
        Assert.Contains("os_http_request_duration_seconds_bucket{le=\"+Inf\"}", metrics);
        Assert.DoesNotContain("@", metrics);
    }

    [Fact]
    public async Task BackupCanBeRestoredWithoutChangingSourceDatabase()
    {
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<OsDb>();
        var userCount = await database.Users.LongCountAsync();
        var service = new SqlBackupService(database.Database.GetConnectionString()!);
        var path = await service.CreateAsync();
        var result = await service.VerifyRestoreAsync(path);
        Assert.Equal(userCount, result.Rows["Users"]);
        Assert.Equal((await database.Database.GetAppliedMigrationsAsync()).LongCount(), result.Rows["__EFMigrationsHistory"]);
        Assert.Equal(userCount, await database.Users.LongCountAsync());
        Assert.StartsWith("OsRestore_", result.TestDatabase);
        var connection = database.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT DB_ID(@target)";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@target";
        parameter.Value = result.TestDatabase;
        command.Parameters.Add(parameter);
        Assert.Equal(DBNull.Value, await command.ExecuteScalarAsync());
    }

    private static async Task<LoginResponse> Login(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, SqlServerApiFactory.Password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>(SqlServerApiFactory.Json))!;
    }
}
