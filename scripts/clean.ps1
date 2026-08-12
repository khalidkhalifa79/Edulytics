$ErrorActionPreference = 'Stop'

Write-Host 'Cleaning Edulytics build artifacts...'
Get-ChildItem -Path . -Recurse -Directory -Filter bin | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Get-ChildItem -Path . -Recurse -Directory -Filter obj | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
