<#
.SYNOPSIS
    Automated Standalone Production Build & Installer Packager for Sasan The Gadget Lounge ERP.
.DESCRIPTION
    Compiles, publishes, and packages a self-contained, single-file Windows x64 binary with zero
    external runtime dependencies, ready for retail store deployment.
#>

param (
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputDir = "dist\win-x64"
)

$ErrorActionPreference = "Stop"

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " 🚀 SASAN THE GADGET LOUNGE - STANDALONE INSTALLER PACKAGER" -ForegroundColor Yellow
Write-Host "=================================================================" -ForegroundColor Cyan

$projectPath = "src\SasanERP.UI\SasanERP.UI.csproj"
if (-not (Test-Path $projectPath)) {
    Write-Error "Project file not found at: $projectPath"
}

# 1. Clean & Prepare Target Directories
Write-Host "[1/4] Preparing output directory: $OutputDir..." -ForegroundColor Gray
if (Test-Path $OutputDir) {
    Remove-Item -Recurse -Force $OutputDir
}
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# 2. Compile & Publish Self-Contained Single-File Executable
Write-Host "[2/4] Publishing self-contained $Runtime executable..." -ForegroundColor Yellow
dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $OutputDir

# 3. Verify Output Executable
$exePath = Join-Path $OutputDir "SasanERP.UI.exe"
if (-not (Test-Path $exePath)) {
    Write-Error "Publish failed: $exePath was not generated."
}

$fileInfo = Get-Item $exePath
$fileSizeMB = [math]::Round($fileInfo.Length / 1MB, 2)
$sha256 = (Get-FileHash -Path $exePath -Algorithm SHA256).Hash

Write-Host "[3/4] Binary generated successfully: $exePath ($fileSizeMB MB)" -ForegroundColor Green
Write-Host "      SHA-256 Checksum: $sha256" -ForegroundColor DarkGray

# 4. Generate Manifest & Release Package Metadata
Write-Host "[4/4] Writing release manifest..." -ForegroundColor Gray
$manifestPath = Join-Path $OutputDir "release-manifest.json"
$manifest = [ordered]@{
    Application = "Sasan The Gadget Lounge ERP"
    Version = "2.5.0"
    BuildDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
    Runtime = $Runtime
    Configuration = $Configuration
    Executable = "SasanERP.UI.exe"
    SizeMB = $fileSizeMB
    SHA256 = $sha256
    ZeroDependencies = $true
    TargetOS = "Windows 10 / Windows 11 (x64)"
}
$manifest | ConvertTo-Json -Depth 4 | Set-Content -Path $manifestPath -Encoding UTF8

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " ✅ STANDALONE PRODUCTION BUILD COMPLETED SUCCESSFULLY!" -ForegroundColor Green
Write-Host " Target Binary: $exePath" -ForegroundColor White
Write-Host " Size: $fileSizeMB MB | SHA-256: $sha256" -ForegroundColor Gray
Write-Host "=================================================================" -ForegroundColor Cyan
