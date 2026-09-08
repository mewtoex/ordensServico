param(
    [uri]$BaseUrl = 'http://localhost:5080',
    [ValidateRange(1, 60)][int]$TimeoutSeconds = 10,
    [ValidateRange(1, 60000)][int]$MaxLatencyMs = 2000
)
$ErrorActionPreference = 'Stop'
$results = @()
foreach ($endpoint in @('/health/live', '/health')) {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    try {
        $response = Invoke-WebRequest -Uri ([uri]::new($BaseUrl, $endpoint)) -TimeoutSec $TimeoutSeconds
        $healthy = $response.StatusCode -eq 200
        if ($endpoint -eq '/health') { $healthy = $healthy -and (($response.Content | ConvertFrom-Json).status -eq 'ok') }
    }
    catch { $healthy = $false }
    $watch.Stop()
    $results += [pscustomobject]@{ Endpoint = $endpoint; Healthy = $healthy; ElapsedMs = $watch.ElapsedMilliseconds }
}
$ok = @($results | Where-Object { -not $_.Healthy -or $_.ElapsedMs -gt $MaxLatencyMs }).Count -eq 0
[pscustomobject]@{ At = [DateTimeOffset]::UtcNow.ToString('o'); Healthy = $ok; Checks = $results } | ConvertTo-Json -Depth 4
if (-not $ok) { exit 1 }
