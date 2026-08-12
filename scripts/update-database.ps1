$ErrorActionPreference = 'Stop'

Write-Host 'Updating Edulytics database schema...'

# Check if connection string is provided as argument
if ($args.Count -eq 0) {
    Write-Host 'Usage: .\scripts\update-database.ps1 -ConnectionString "<connection-string>"'
    Write-Host ''
    Write-Host 'Example:'
    Write-Host '  .\scripts\update-database.ps1 -ConnectionString "Server=(localdb)\MSSQLLocalDB;Database=EdulyticsDev;Trusted_Connection=True;MultipleActiveResultSets=true"'
    Write-Host ''
    Write-Host 'Connection string can also be read from:'
    Write-Host '  1. Environment variable: EDULYTICS_CONNECTION_STRING'
    Write-Host '  2. Azure Key Vault / secure configuration'
    Write-Host '  3. appsettings.Development.json (for local development only)'
    Write-Host ''
    exit 1
}

# Parse named parameter
$ConnectionString = $null
if ($args[0] -like '-ConnectionString*') {
    if ($args.Count -gt 1) {
        $ConnectionString = $args[1]
    }
    else {
        Write-Error 'Connection string value required after -ConnectionString parameter'
        exit 1
    }
}
elseif ([string]::IsNullOrWhiteSpace($env:EDULYTICS_CONNECTION_STRING)) {
    Write-Error 'Connection string must be provided via -ConnectionString parameter or EDULYTICS_CONNECTION_STRING environment variable'
    exit 1
}
else {
    $ConnectionString = $env:EDULYTICS_CONNECTION_STRING
}

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    Write-Error 'Connection string cannot be empty'
    exit 1
}

Write-Host "Applying migrations to database..."
Write-Host ""

# Run EF database update
dotnet ef database update `
    --project "src/Edulytics.Data/Edulytics.Data.csproj" `
    --startup-project "src/Edulytics.Web/Edulytics.Web.csproj" `
    --connection "$ConnectionString"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Database update failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "✓ Database update completed successfully."
