using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Os.Api.Application;
using Os.Api.Domain;
using Os.Api.Infra;
using Xunit;

namespace Os.IntegrationTests;

public sealed class SqlServerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Password = "Integration-Only-Password123!";
    public const string AdminEmail = "admin@integration.example";
    public const string AllowedOrigin = "https://frontend.integration.example";
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    public readonly ConcurrentSaveGate SaveGate = new();
    private readonly string _databaseName = "OsIntegration_" + Guid.NewGuid().ToString("N");
    private readonly string _connectionString;
    private bool _created;
    public HttpClient Admin { get; private set; } = null!;

    public SqlServerApiFactory() : this(null) { }

    internal SqlServerApiFactory(string? connectionOverride)
    {
        var connection = connectionOverride ?? Environment.GetEnvironmentVariable("IntegrationTests__ConnectionString")
            ?? throw new InvalidOperationException("Configure IntegrationTests__ConnectionString para um SQL Server de testes com permissão de criar bancos.");
        var builder = new SqlConnectionStringBuilder(connection)
        {
            InitialCatalog = _databaseName,
            ConnectTimeout = 3
        };
        _connectionString = builder.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("RateLimiting:LoginPermitLimit", "100");
        builder.UseSetting("ConnectionStrings:Database", _connectionString);
        builder.UseSetting("Jwt:Key", "integration-only-signing-key-at-least-32-bytes");
        builder.UseSetting("Cors:AllowedOrigins:0", AllowedOrigin);
        builder.UseSetting("Cors:AllowedOrigins:1", "https://secondary.integration.example");
        builder.ConfigureServices(services => services.AddDbContext<OsDb>(options => options.AddInterceptors(SaveGate)));
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<OsDb>();
        _created = true;
        await database.Database.MigrateAsync();
        var admin = new User { Name = "Integration Admin", Email = AdminEmail, Role = Role.Admin };
        admin.PasswordHash = new PasswordHasher<User>().HashPassword(admin, Password);
        database.Users.Add(admin);
        await database.SaveChangesAsync();
        Admin = await LoginAsync(AdminEmail);
    }

    public async Task<HttpClient> LoginAsync(string email)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, Password), Json);
        response.EnsureSuccessStatusCode();
        var login = (await response.Content.ReadFromJsonAsync<LoginResponse>(Json))!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    public async Task<(UserResponse User, HttpClient Client)> CreateTechnicianAsync()
    {
        var response = await Admin.PostAsJsonAsync("/api/users",
            new UserRequest("Técnico", Guid.NewGuid().ToString("N") + "@integration.example", Password, Role.Tecnico), Json);
        response.EnsureSuccessStatusCode();
        var user = (await response.Content.ReadFromJsonAsync<UserResponse>(Json))!;
        return (user, await LoginAsync(user.Email));
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        try
        {
            if (_created)
            {
                using var scope = Services.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<OsDb>();
                var target = new SqlConnectionStringBuilder(database.Database.GetConnectionString()).InitialCatalog;
                if (target != _databaseName || !target.StartsWith("OsIntegration_", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Recusando remover banco que não pertence a este teste.");
                }
                await database.Database.EnsureDeletedAsync();
            }
        }
        finally
        {
            Admin?.Dispose();
            await base.DisposeAsync();
        }
    }
}
