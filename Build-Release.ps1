# Build one Release of EliteFIPServer.UI and package it for both channels, so the Microsoft Store
# package and the GitHub Releases installer contain the same build with the same version number:
#   1. publish once (this bumps the build number, like every UI build)
#   2. check the build starts and serves the dashboard
#   3. Build-StorePackage.ps1 -SkipBuild  -> artifacts\msix\EliteFIPServer.msix
#   4. Build-Installer.ps1 -SkipBuild     -> artifacts\installer\Releases\...Setup.exe
#
# Packaging only one channel later (by running its script on its own) builds again and gives it a newer
# number, so release both together.
#
# -DownloadPrevious and -Publish are passed to Build-Installer.ps1 (see that script). -Publish uploads a
# draft GitHub release; the Store package is always uploaded to Partner Center by hand.
#
# Prerequisites: those of both scripts (StoreIdentity.local.json, dotnet tool restore).

param(
    [switch]$DownloadPrevious,
    [switch]$Publish
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$uiProject = Join-Path $root "EliteFIPServer.UI\EliteFIPServer.UI.csproj"
$contentDir = Join-Path $root "EliteFIPServer.UI\bin\x64\Release\net10.0-windows10.0.19041.0\win-x64"
$versionPropsPath = Join-Path $root "EliteFIPServer.Version.props"

Write-Host "Stopping any running EliteFIPServer.exe..." -ForegroundColor Cyan
Get-Process -Name "EliteFIPServer" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

Write-Host "Publishing EliteFIPServer.UI (Release, x64)..." -ForegroundColor Cyan
dotnet publish $uiProject -c Release -p:Platform=x64 --nologo -v:minimal
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$version = ([xml](Get-Content $versionPropsPath -Raw)).Project.PropertyGroup.Version
Write-Host "Built version $version" -ForegroundColor Cyan

# A packaging mistake (such as missing *.xbf or EliteFIPServer.pri) shows up as a crash at startup.
Write-Host "Checking the build starts and serves the dashboard..." -ForegroundColor Cyan
$app = Start-Process (Join-Path $contentDir "EliteFIPServer.exe") -ArgumentList "--start-panel-server" -PassThru
try {
    Start-Sleep -Seconds 15
    if ($app.HasExited) {
        throw "EliteFIPServer.exe exited during the startup check (exit code $($app.ExitCode)). See the Windows Application event log."
    }

    $port = 4545
    $settingsPath = Join-Path $env:APPDATA "EliteFIPServer\settings.json"
    if (Test-Path $settingsPath) {
        $savedPort = (Get-Content $settingsPath -Raw | ConvertFrom-Json).PanelServerPort
        if ($savedPort) { $port = $savedPort }
    }

    $status = (Invoke-WebRequest "http://127.0.0.1:$port/" -UseBasicParsing -TimeoutSec 10).StatusCode
    if ($status -ne 200) {
        throw "The dashboard returned HTTP $status during the startup check."
    }
}
finally {
    Stop-Process -Id $app.Id -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
}

& (Join-Path $root "Build-StorePackage.ps1") -SkipBuild
& (Join-Path $root "Build-Installer.ps1") -SkipBuild -DownloadPrevious:$DownloadPrevious -Publish:$Publish

Write-Host ""
Write-Host "Store package and installer both built from version $version." -ForegroundColor Green
