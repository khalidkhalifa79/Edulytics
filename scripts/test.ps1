$ErrorActionPreference = 'Stop'

Write-Host 'Running Edulytics tests...'
dotnet test Edulytics.sln --nologo --verbosity minimal
