using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Os.Api.Application.Abstractions;
using Os.Api.Application.Interfaces.Repositories;
using Os.Api.Application.Interfaces.Services;
using Os.Api.Application.Services;
using Os.Api.Domain;
using Os.Api.Infra;
using Os.Api.Infra.Auth;
using Os.Api.Infra.Documents;
using Os.Api.Infra.Health;
using Os.Api.Infra.Repositories;

namespace Os.Api.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddBackend(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OsDb>(options => options.UseSqlServer(configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Configure ConnectionStrings__Database.")));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IRefreshSessionRepository, RefreshSessionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ITokenIssuer, JwtTokenIssuer>();
        services.AddScoped<ActiveUserTokenEvents>();
        services.AddScoped<AdminSeeder>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsersService, UsersService>();
        services.AddScoped<ICustomersService, CustomersService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IOrdersService, OrdersService>();
        services.AddScoped<IReportsService, ReportsService>();
        services.AddScoped<IOrderPdfService, OrderPdfService>();
        services.AddScoped<IOrderPdfRenderer, OrderPdfRenderer>();
        services.AddScoped<IOrderSummaryService, OrderSummaryService>();
        services.AddControllers().AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
        services.AddEndpointsApiExplorer();
        services.AddFrontendCors(configuration);
        services.AddApiDocumentation();
        services.AddHealthChecks().AddCheck<SqlServerHealthCheck>("sqlserver", timeout: TimeSpan.FromSeconds(5));
        services.AddJwtAuthentication(configuration);
        services.AddAuthorization();
        var loginPermitLimit = configuration.GetValue<int?>("RateLimiting:LoginPermitLimit") ?? 10;
        if (loginPermitLimit is < 1 or > 1000)
            throw new InvalidOperationException("RateLimiting:LoginPermitLimit deve estar entre 1 e 1000.");
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = loginPermitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
        });
        return services;
    }
    private static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Configure Jwt__Key com pelo menos 32 caracteres.");
        if (Encoding.UTF8.GetByteCount(key) < 32)
        {
            throw new InvalidOperationException("Jwt__Key deve ter pelo menos 32 bytes.");
        }
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ValidateIssuer = true,
                ValidIssuer = JwtTokenIssuer.Issuer,
                ValidateAudience = true,
                ValidAudience = JwtTokenIssuer.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
            options.EventsType = typeof(ActiveUserTokenEvents);
        });
        return services;
    }
}
