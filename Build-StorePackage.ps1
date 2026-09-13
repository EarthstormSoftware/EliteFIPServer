# Build a Microsoft Store-ready .msix by publishing EliteFIPServer.UI and packaging the output
# directly with MakeAppx.exe. This bypasses the automated MSBuild single-project MSIX pipeline
# (GenerateAppxPackageOnBuild / PublishAppxPackage) entirely — that pipeline never reliably invokes
# actual packaging for this project on the .NET 10 SDK, and opting into it (WindowsPackageType=MSIX /
# EnableMsixTooling=true) previously broke the normal loose-exe build too. EliteFIPServer.UI.csproj
# stays a plain WindowsPackageType=None project; publish here is just a normal self-contained publish.
# See docs/HANDOFF.md for the full investigation and incident writeup.
#
# Prerequisites:
#   - EliteFIPServer.UI\StoreIdentity.local.json must exist with real Partner Center values
#     (copy StoreIdentity.local.json.template and fill it in; this file is gitignored).
#   - No running EliteFIPServer.exe (its output files would be locked during publish).

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$uiProject = Join-Path $root "EliteFIPServer.UI\EliteFIPServer.UI.csproj"
$identityJsonPath = Join-Path $root "EliteFIPServer.UI\StoreIdentity.local.json"
$sourceManifestPath = Join-Path $root "EliteFIPServer.UI\Package.appxmanifest"
$versionPropsPath = Join-Path $root "EliteFIPServer.Version.props"
$generateManifestScript = Join-Path $root "Generate-StoreManifest.ps1"
$storeBuildToolsProject = Join-Path $root "tools\StoreBuildTools\StoreBuildTools.csproj"
$packageOutputDir = Join-Path $root "artifacts\msix"
$packageOutputPath = Join-Path $packageOutputDir "EliteFIPServer.msix"

if (-not (Test-Path $identityJsonPath)) {
    throw "Missing $identityJsonPath. Copy EliteFIPServer.UI\StoreIdentity.local.json.template to StoreIdentity.local.json and fill in your real Partner Center values first."
}

Write-Host "Stopping any running EliteFIPServer.exe..." -ForegroundColor Cyan
Get-Process -Name "EliteFIPServer" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

Write-Host "Publishing EliteFIPServer.UI ($Configuration, x64)..." -ForegroundColor Cyan
dotnet publish $uiProject -c $Configuration -p:Platform=x64 --nologo -v:minimal
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$contentDir = Join-Path $root "EliteFIPServer.UI\bin\x64\$Configuration\net10.0-windows10.0.19041.0\win-x64"
if (-not (Test-Path $contentDir)) {
    throw "Expected publish output directory not found: $contentDir"
}

# dotnet publish also creates a nested `publish\` subfolder inside this directory (a filtered,
# separate deployment layout). It must not be included in the package payload.
$nestedPublishDir = Join-Path $contentDir "publish"
if (Test-Path $nestedPublishDir) {
    Remove-Item $nestedPublishDir -Recurse -Force
}

Write-Host "Generating Store manifest with local identity..." -ForegroundColor Cyan
$outputManifestPath = Join-Path $contentDir "AppxManifest.xml"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File $generateManifestScript `
    -SourceManifestPath $sourceManifestPath `
    -OutputManifestPath $outputManifestPath `
    -VersionPropsPath $versionPropsPath `
    -IdentityJsonPath $identityJsonPath
if ($LASTEXITCODE -ne 0) {
    throw "Generate-StoreManifest.ps1 failed with exit code $LASTEXITCODE."
}

Write-Host "Ensuring a current Microsoft.Windows.SDK.BuildTools (MakeAppx.exe) is restored..." -ForegroundColor Cyan
dotnet restore $storeBuildToolsProject --nologo -v:minimal
if ($LASTEXITCODE -ne 0) {
    throw "Restoring $storeBuildToolsProject failed with exit code $LASTEXITCODE."
}

$makeAppxCandidates = Get-ChildItem -Path "$env:USERPROFILE\.nuget\packages\microsoft.windows.sdk.buildtools" -Directory -ErrorAction SilentlyContinue |
    Sort-Object { [version]($_.Name -replace '^(\d+\.\d+\.\d+).*', '$1.0') } -Descending
$makeAppx = $null
foreach ($candidate in $makeAppxCandidates) {
    $found = Get-ChildItem -Path $candidate.FullName -Recurse -Filter "makeappx.exe" -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -like "*\x64\*" } | Select-Object -First 1
    if ($found) {
        $makeAppx = $found.FullName
        break
    }
}
if (-not $makeAppx) {
    throw "Could not locate makeappx.exe under $env:USERPROFILE\.nuget\packages\microsoft.windows.sdk.buildtools\*\bin\*\x64\. Ensure the Microsoft.Windows.SDK.BuildTools package has been restored."
}
Write-Host "Using MakeAppx: $makeAppx" -ForegroundColor DarkGray

if (-not (Test-Path $packageOutputDir)) {
    New-Item -ItemType Directory -Path $packageOutputDir -Force | Out-Null
}

Write-Host "Packing MSIX..." -ForegroundColor Cyan
& $makeAppx pack /d $contentDir /p $packageOutputPath /o
if ($LASTEXITCODE -ne 0) {
    throw "MakeAppx pack failed with exit code $LASTEXITCODE."
}

$package = Get-Item $packageOutputPath
Write-Host ""
Write-Host "Package created: $($package.FullName)" -ForegroundColor Green
Write-Host "Size: $([math]::Round($package.Length / 1MB, 1)) MB" -ForegroundColor Green
Write-Host ""
Write-Host "This package is unsigned. Microsoft Partner Center re-signs packages on submission," -ForegroundColor Yellow
Write-Host "so upload it as-is." -ForegroundColor Yellow
