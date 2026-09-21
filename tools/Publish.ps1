# Publish CaYaVidFit single-file exe into .\dist
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $Root 'src\CaYaVidFit\CaYaVidFit.csproj'))) {
  $Root = Split-Path -Parent $PSScriptRoot
}
$Proj = Join-Path $Root 'src\CaYaVidFit\CaYaVidFit.csproj'
$Dist = Join-Path $Root 'dist'
Get-Process -Name 'CaYaVidFit' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $Dist | Out-Null
dotnet publish $Proj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $Dist
if ($LASTEXITCODE -ne 0) { throw "publish failed: $LASTEXITCODE" }
Write-Host "OK ->" (Join-Path $Dist 'CaYaVidFit.exe')
