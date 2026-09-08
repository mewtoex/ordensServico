param(
    [switch]$StartDatabase,
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$environmentFile = Join-Path $projectRoot '.env'

if (Test-Path -LiteralPath $environmentFile) {
    foreach ($line in Get-Content -LiteralPath $environmentFile) {
        if ($line -match '^(DB_PASSWORD|ConnectionStrings__Database|Jwt__Key)=(.*)$') {
            [Environment]::SetEnvironmentVariable($Matches[1], $Matches[2], 'Process')
        }
    }
}

if (-not $env:ConnectionStrings__Database) {
    throw 'Configure ConnectionStrings__Database no ambiente ou no arquivo .env.'
}

Push-Location $projectRoot
try {
    if ($StartDatabase) {
        docker compose up -d database
        if ($LASTEXITCODE -ne 0) {
            throw 'Não foi possível iniciar o SQL Server. Verifique se o Docker está ativo.'
        }
    }

    $migrationArguments = @('ef', 'database', 'update', '--project', 'src/Os.Api', '--configuration', 'Release')
    if ($NoBuild) {
        $migrationArguments += '--no-build'
    }
    dotnet @migrationArguments
    if ($LASTEXITCODE -ne 0) {
        throw 'Falha ao aplicar migrations. Verifique se o SQL Server está pronto e a conexão está correta.'
    }
}
finally {
    Pop-Location
}
