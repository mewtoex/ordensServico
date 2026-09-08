param([switch]$NoBuild)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not $env:ConnectionStrings__Database -and (Test-Path -LiteralPath (Join-Path $projectRoot '.env'))) {
    foreach ($line in Get-Content -LiteralPath (Join-Path $projectRoot '.env')) {
        if ($line -match '^ConnectionStrings__Database=(.*)$') { $env:ConnectionStrings__Database = $Matches[1] }
    }
}
Push-Location $projectRoot
try {
    $container = docker compose ps -q database
    if ($LASTEXITCODE -ne 0 -or -not $container) { throw 'Inicie o SQL Server com docker compose up -d database.' }
    if (-not $NoBuild) {
        dotnet build tools/Os.DatabaseTools --configuration Release --no-restore
        if ($LASTEXITCODE -ne 0) { throw 'Falha no build da ferramenta de backup.' }
    }
    $output = dotnet run --project tools/Os.DatabaseTools --configuration Release --no-build -- backup
    if ($LASTEXITCODE -ne 0) { throw 'Falha no backup.' }
    $backup = $output | ConvertFrom-Json
    $destination = Join-Path $projectRoot 'backups'
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    $file = Join-Path $destination ([IO.Path]::GetFileName($backup.BackupPath))
    if (Test-Path -LiteralPath $file) { throw 'O arquivo de destino já existe.' }
    docker cp "${container}:$($backup.BackupPath)" $file
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao copiar backup para o host.' }
    $report = & (Join-Path $PSScriptRoot 'Test-Restore.ps1') -BackupPath $file -NoBuild
    $report | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath "$file.json"
    Write-Output "Backup restaurado e validado: $file"
}
finally { Pop-Location }
