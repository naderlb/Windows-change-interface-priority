param(
    [string]$Runtime = "win-x64",
    [string]$Out = "publish-portable"
)

$ErrorActionPreference = "Stop"
$proj = Join-Path $PSScriptRoot "InterfaceMetricSelector.csproj"
$outAbs = Join-Path $PSScriptRoot $Out

dotnet publish $proj -c Release -r $Runtime `
  --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  /p:EnableCompressionInSingleFile=true `
  /p:PublishTrimmed=false `
  /p:DebugType=embedded `
  -o $outAbs

Write-Host ""
Write-Host "Output: $outAbs\InterfaceMetricSelector.exe" -ForegroundColor Green
Write-Host "No .NET runtime install required on target Windows x64 machines." -ForegroundColor Cyan
