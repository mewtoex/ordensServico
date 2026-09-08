using System.Diagnostics;
using Microsoft.AspNetCore.Routing;
using Os.Api.Infra.Monitoring;

namespace Os.Api.Middleware;

public sealed class RequestTelemetryMiddleware(RequestDelegate next, ILogger<RequestTelemetryMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, RequestMetrics metrics)
    {
        var started = Stopwatch.GetTimestamp();
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        context.Response.Headers["X-Trace-Id"] = traceId;
        try
        {
            await next(context);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(started);
            metrics.Record(context.Response.StatusCode, elapsed.TotalSeconds);
            var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";
            // Log the route template only; never log bodies, query strings or headers.
            logger.LogInformation("HTTP {Method} {Route} responded {StatusCode} in {ElapsedMs} ms; trace {TraceId}",
                context.Request.Method, route, context.Response.StatusCode, elapsed.TotalMilliseconds, traceId);
        }
    }
}
