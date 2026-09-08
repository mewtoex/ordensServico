param([switch]$NoBuild)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot

if (-not $env:IntegrationTests__ConnectionString) {
    $environmentFile = Join-Path $projectRoot '.env'
    if (Test-Path -LiteralPath $environmentFile) {
        foreach ($line in Get-Content -LiteralPath $environmentFile) {
            if ($line -match '^ConnectionStrings__Database=(.*)$') {
                $env:IntegrationTests__ConnectionString = $Matches[1]
            }
        }
    }
}

if (-not $env:IntegrationTests__ConnectionString) {
    throw 'Configure IntegrationTests__ConnectionString ou a conexão local em .env.'
}

Push-Location $projectRoot
try {
    $testArguments = @('test', 'tests/Os.IntegrationTests', '--configuration', 'Release', '--no-restore',
        '--logger', 'trx', '--results-directory', 'TestResults')
    if ($NoBuild) {
        $testArguments += '--no-build'
    }
    dotnet @testArguments
    if ($LASTEXITCODE -ne 0) {
        throw 'Os testes de integração falharam. Consulte os resultados em TestResults.'
    }
}
finally {
    Pop-Location
}
