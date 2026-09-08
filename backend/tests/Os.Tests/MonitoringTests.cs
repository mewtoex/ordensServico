using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Os.Api.Infra.Monitoring;
using Os.Api.Middleware;
using Xunit;

namespace Os.Tests;

public class MonitoringTests
{
    [Fact]
    public async Task TelemetryDoesNotRecordCredentialsOrQueryStrings()
    {
        var logger = new CaptureLogger();
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/private-customer-name";
        context.Request.QueryString = new QueryString("?password=secret-query");
        context.Request.Headers.Authorization = "Bearer secret-token";
        context.Request.Body = new MemoryStream("secret-body"u8.ToArray());
        var metrics = new RequestMetrics();
        var middleware = new RequestTelemetryMiddleware(current =>
        {
            current.Response.StatusCode = 503;
            return Task.CompletedTask;
        }, logger);
        await middleware.InvokeAsync(context, metrics);
        Assert.False(string.IsNullOrEmpty(context.Response.Headers["X-Trace-Id"]));
        Assert.Contains("503", logger.Message);
        Assert.DoesNotContain("secret", logger.Message);
        Assert.DoesNotContain("private-customer", logger.Message);
        Assert.Equal(0, context.Request.Body.Position);
        Assert.Contains("os_http_requests_total{status_class=\"5xx\"} 1", metrics.Export());
    }

    [Fact]
    public void MetricsAccumulateConcurrentRequestsAndCumulativeBuckets()
    {
        var metrics = new RequestMetrics();
        Parallel.For(0, 1000, _ => metrics.Record(200, 0.2));
        var output = metrics.Export();
        Assert.Contains("os_http_request_duration_seconds_count 1000", output);
        Assert.Contains("os_http_request_duration_seconds_bucket{le=\"0.1\"} 0", output);
        Assert.Contains("os_http_request_duration_seconds_bucket{le=\"0.25\"} 1000", output);
        Assert.Contains("os_http_request_duration_seconds_bucket{le=\"+Inf\"} 1000", output);
    }

    private sealed class CaptureLogger : ILogger<RequestTelemetryMiddleware>
    {
        public string Message { get; private set; } = "";
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Message = formatter(state, exception);
    }
}
