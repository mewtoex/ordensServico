param(
    [Parameter(Mandatory)][string]$BackupPath,
    [switch]$NoBuild
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$file = (Resolve-Path -LiteralPath $BackupPath).Path
if ([IO.Path]::GetExtension($file) -ne '.bak') { throw 'Informe um arquivo .bak.' }
if (-not $env:ConnectionStrings__Database -and (Test-Path -LiteralPath (Join-Path $projectRoot '.env'))) {
    foreach ($line in Get-Content -LiteralPath (Join-Path $projectRoot '.env')) {
        if ($line -match '^ConnectionStrings__Database=(.*)$') { $env:ConnectionStrings__Database = $Matches[1] }
    }
}
Push-Location $projectRoot
try {
    $container = docker compose ps -q database
    if ($LASTEXITCODE -ne 0 -or -not $container) { throw 'Inicie o SQL Server local.' }
    if (-not $NoBuild) {
        dotnet build tools/Os.DatabaseTools --configuration Release --no-restore | Out-Host
        if ($LASTEXITCODE -ne 0) { throw 'Falha no build da ferramenta de backup.' }
    }
    $serverPath = '/var/opt/mssql/data/osbackup-' + [Guid]::NewGuid().ToString('N') + '.bak'
    docker cp $file "${container}:$serverPath" | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao copiar o arquivo para o teste de restauração.' }
    $output = dotnet run --project tools/Os.DatabaseTools --configuration Release --no-build -- verify $serverPath
    if ($LASTEXITCODE -ne 0) { throw 'O teste de restauração falhou.' }
    $report = $output | ConvertFrom-Json
    $report | Add-Member -NotePropertyName Sha256 -NotePropertyValue (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash
    $report | Add-Member -NotePropertyName VerifiedAt -NotePropertyValue ([DateTimeOffset]::UtcNow.ToString('o'))
    $report
}
finally { Pop-Location }
