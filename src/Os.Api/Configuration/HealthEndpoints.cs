using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Os.Api.Configuration;

public static class HealthEndpoints
{
    public static void MapBackendHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = (context, report) => context.Response.WriteAsJsonAsync(new
            {
                status = report.Status == HealthStatus.Healthy ? "ok" : "unhealthy",
                checks = report.Entries.ToDictionary(entry => entry.Key, entry => entry.Value.Status.ToString())
            })
        });
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
    }
}
