using System.Globalization;
using System.Text;

namespace Os.Api.Infra.Monitoring;

// Bounded, process-local counters. No URL, user, token or customer labels.
public sealed class RequestMetrics
{
    private readonly object gate = new();
    private readonly double[] bounds = [0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10];
    private readonly long[] buckets = new long[8];
    private readonly long[] statuses = new long[6];
    private long count;
    private double seconds;

    public void Record(int statusCode, double elapsedSeconds)
    {
        lock (gate)
        {
            count++;
            seconds += elapsedSeconds;
            statuses[Math.Clamp(statusCode / 100, 1, 5)]++;
            for (var i = 0; i < bounds.Length; i++)
                if (elapsedSeconds <= bounds[i])
                    buckets[i]++;
        }
    }

    public string Export()
    {
        lock (gate)
        {
            var result = new StringBuilder("# HELP os_http_requests_total Completed HTTP requests.\n# TYPE os_http_requests_total counter\n");
            for (var i = 1; i <= 5; i++)
                result.AppendLine($"os_http_requests_total{{status_class=\"{i}xx\"}} {statuses[i]}");
            result.AppendLine("# HELP os_http_request_duration_seconds HTTP request duration.\n# TYPE os_http_request_duration_seconds histogram");
            for (var i = 0; i < bounds.Length; i++)
                result.AppendLine($"os_http_request_duration_seconds_bucket{{le=\"{bounds[i].ToString(CultureInfo.InvariantCulture)}\"}} {buckets[i]}");
            result.AppendLine($"os_http_request_duration_seconds_bucket{{le=\"+Inf\"}} {count}");
            result.AppendLine($"os_http_request_duration_seconds_sum {seconds.ToString(CultureInfo.InvariantCulture)}");
            result.AppendLine($"os_http_request_duration_seconds_count {count}");
            return result.ToString().Replace("\r\n", "\n");
        }
    }
}
