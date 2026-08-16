$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

$connection = $env:EDULYTICS_MIGRATION_CONNECTION
if ([string]::IsNullOrWhiteSpace($connection)) {
    $connection = [Environment]::GetEnvironmentVariable('ConnectionStrings__MigrationConnection')
}
if ([string]::IsNullOrWhiteSpace($connection)) {
    throw 'Migration connection is required. Use the Neon direct/non-pooler PostgreSQL endpoint.'
}

$env:ConnectionStrings__MigrationConnection = $connection
$env:ConnectionStrings__DefaultConnection = $connection

dotnet ef database update `
    --project 'src/Edulytics.Data/Edulytics.Data.csproj' `
    --startup-project 'src/Edulytics.Web/Edulytics.Web.csproj' `
    --context 'EdulyticsDbContext'

if ($LASTEXITCODE -ne 0) { throw "Database update failed with exit code $LASTEXITCODE" }
Write-Host 'PostgreSQL database migration completed successfully.'
