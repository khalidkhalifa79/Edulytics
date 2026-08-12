$ErrorActionPreference = 'Stop'

Write-Host 'Building Edulytics solution...'
dotnet build Edulytics.sln --nologo --verbosity minimal
