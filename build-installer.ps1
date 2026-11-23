<#
.SYNOPSIS
    Automated Standalone Production Build & Installer Packager for Sasan The Gadget Lounge ERP.
.DESCRIPTION
    1. Compiles and publishes a self-contained, single-file Windows x64 binary with zero external runtime dependencies.
    2. Packages release manifest with SHA-256 verification.
    3. Compiles a professional Windows Setup Wizard (Setup.exe) using Inno Setup 6.
    4. Generates a portable distribution zip archive.
#>

param (
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputDir = "dist\win-x64",
    [string]$InstallerOutputDir = "dist\installer"
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
Write-Host "[1/6] Preparing output directories..." -ForegroundColor Gray
if (Test-Path $OutputDir) {
    Remove-Item -Recurse -Force $OutputDir
}
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
New-Item -ItemType Directory -Force -Path $InstallerOutputDir | Out-Null

# 2. Build React 18 + Vite + Tailwind Luxury WebUI
Write-Host "[2/6] Compiling luxury React 18 + Tailwind SPA..." -ForegroundColor Cyan
npm --prefix src\SasanERP.WebUI run build
if (-not (Test-Path "src\SasanERP.WebUI\dist\index.html")) {
    Write-Error "React UI build failed: dist\index.html not found."
}
New-Item -ItemType Directory -Force -Path "src\SasanERP.Server\wwwroot" | Out-Null
Copy-Item -Path "src\SasanERP.WebUI\dist\*" -Destination "src\SasanERP.Server\wwwroot" -Recurse -Force

# 3. Compile & Publish Self-Contained Single-File Executable
Write-Host "[3/6] Publishing self-contained $Runtime executable..." -ForegroundColor Yellow
dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $OutputDir

# Copy wwwroot to publish output directory for offline WebView2 access
Copy-Item -Path "src\SasanERP.Server\wwwroot" -Destination "$OutputDir\wwwroot" -Recurse -Force

# 4. Verify Output Executable
$exePath = Join-Path $OutputDir "SasanERP.UI.exe"
if (-not (Test-Path $exePath)) {
    Write-Error "Publish failed: $exePath was not generated."
}

$fileInfo = Get-Item $exePath
$fileSizeMB = [math]::Round($fileInfo.Length / 1MB, 2)
$sha256 = (Get-FileHash -Path $exePath -Algorithm SHA256).Hash

Write-Host "[4/6] Standalone binary generated: $exePath ($fileSizeMB MB)" -ForegroundColor Green
Write-Host "      SHA-256 Checksum: $sha256" -ForegroundColor DarkGray

# 5. Generate Manifest & Release Package Metadata
Write-Host "[5/6] Writing release manifest..." -ForegroundColor Gray
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

# 6. Compile Inno Setup Windows Installer (.exe)
Write-Host "[6/6] Compiling Windows Setup Installer (.exe)..." -ForegroundColor Yellow

$isccPath = $null
$candidatePaths = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)
foreach ($p in $candidatePaths) {
    if (Test-Path $p) {
        $isccPath = $p
        break
    }
}
if (-not $isccPath) {
    $cmd = Get-Command iscc.exe -ErrorAction SilentlyContinue
    if ($cmd) { $isccPath = $cmd.Source }
}

$installerExe = Join-Path $InstallerOutputDir "SasanERP-Setup-v2.5.0.exe"

if ($isccPath) {
    Write-Host "      Using Inno Setup compiler: $isccPath" -ForegroundColor DarkGray
    $issFile = "packaging\installer.iss"
    & $isccPath /Q $issFile
    
    if (Test-Path $installerExe) {
        $instInfo = Get-Item $installerExe
        $instSizeMB = [math]::Round($instInfo.Length / 1MB, 2)
        $instSha = (Get-FileHash -Path $installerExe -Algorithm SHA256).Hash
        Write-Host "      ✅ Windows Setup Wizard generated: $installerExe ($instSizeMB MB)" -ForegroundColor Green
        Write-Host "         Installer SHA-256: $instSha" -ForegroundColor DarkGray
    } else {
        Write-Warning "Inno Setup ran but $installerExe was not found."
    }
} else {
    Write-Warning "Inno Setup (ISCC.exe) not found. Skipping Setup.exe creation."
}

# 6. Create Portable ZIP Archive
$zipArchive = "dist\SasanERP-v2.5.0-Standalone-win-x64.zip"
if (Test-Path $zipArchive) { Remove-Item -Force $zipArchive }
Compress-Archive -Path "$OutputDir\*" -DestinationPath $zipArchive -CompressionLevel Optimal
$zipInfo = Get-Item $zipArchive
$zipSizeMB = [math]::Round($zipInfo.Length / 1MB, 2)
Write-Host "      ✅ Portable ZIP Archive generated: $zipArchive ($zipSizeMB MB)" -ForegroundColor Green

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " 🎉 COMPLETE PRODUCTION DEPLOYMENT SUITE GENERATED!" -ForegroundColor Green
Write-Host " 1. Windows Installer : $installerExe" -ForegroundColor White
Write-Host " 2. Standalone Binary : $exePath" -ForegroundColor White
Write-Host " 3. Portable Archive  : $zipArchive" -ForegroundColor White
Write-Host "=================================================================" -ForegroundColor Cyan
