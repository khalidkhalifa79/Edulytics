$ErrorActionPreference = 'Stop'

Write-Host 'Restoring Edulytics solution...'
dotnet restore Edulytics.sln --nologo
