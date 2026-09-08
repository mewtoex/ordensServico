using Os.Api.Infra.Monitoring;

namespace Os.Api.Configuration;

public static class MonitoringConfiguration
{
    public static void AddBackendMonitoring(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole(options =>
        {
            // Framework scopes include raw request paths; emit only explicit safe fields.
            options.IncludeScopes = false;
            options.UseUtcTimestamp = true;
            options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
        });
        builder.Services.AddSingleton<RequestMetrics>();
    }

    public static void MapBackendMetrics(this WebApplication app)
    {
        app.MapGet("/metrics", (HttpContext context, RequestMetrics metrics) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return Results.Text(metrics.Export(), "text/plain; version=0.0.4; charset=utf-8");
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
