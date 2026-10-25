# Build the Velopack installer (Setup.exe) for distribution outside the Microsoft Store, from a fresh
# publish of EliteFIPServer.UI. The installer is per-user (%LocalAppData%\EarthstormSoftware.EliteFIPServer,
# no admin needed for the app itself) and unsigned, so Windows SmartScreen warns on first run.
#
# .NET is not bundled into the app. Setup installs the official .NET 10 and ASP.NET Core 10 runtimes
# when they are missing (Windows asks for admin permission for that step only), so Microsoft Update
# keeps them patched like any other .NET install. The Windows App SDK is bundled with the app.
#
# Installed copies check GitHub Releases for updates (AppInstallation.cs). The Store package and dev
# builds never do, so the two update channels don't overlap.
#
# Prerequisites:
#   - dotnet tool restore (vpk is pinned in dotnet-tools.json; keep it at the Velopack package version).
#   - No running EliteFIPServer.exe (its output files would be locked during publish).
#
# Publishing (optional, after testing Setup.exe): pass -Publish with a GitHub token in $env:GITHUB_TOKEN.
# That uploads a draft release tagged v<version>; review and publish it on GitHub. -DownloadPrevious
# fetches the latest published release first so the package includes a delta update from it.

#
# -SkipBuild packages the existing Release build output instead of publishing again. Build-Release.ps1
# uses it so the installer and the Store package come from one build with one version number.

param(
    [switch]$DownloadPrevious,
    [switch]$Publish,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$uiProject = Join-Path $root "EliteFIPServer.UI\EliteFIPServer.UI.csproj"
$versionPropsPath = Join-Path $root "EliteFIPServer.Version.props"
$iconPath = Join-Path $root "EliteFIPServer.UI\Images\EliteFIPServerIcon256.ico"
$publishDir = Join-Path $root "artifacts\installer\publish"
$releasesDir = Join-Path $root "artifacts\installer\Releases"
$repoUrl = "https://github.com/EarthstormSoftware/EliteFIPServer"
$packId = "EarthstormSoftware.EliteFIPServer"

if (-not $SkipBuild) {
    Write-Host "Stopping any running EliteFIPServer.exe..." -ForegroundColor Cyan
    Get-Process -Name "EliteFIPServer" -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 1
}

Push-Location $root
try {
    dotnet tool restore | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet tool restore failed with exit code $LASTEXITCODE."
    }

    # Start from an empty Releases folder, so delta updates are only made against published releases
    # (fetched by -DownloadPrevious), never against local test builds nobody installed.
    foreach ($dir in $publishDir, $releasesDir) {
        if (Test-Path $dir) {
            Remove-Item $dir -Recurse -Force
        }
    }

    if (-not $SkipBuild) {
        # The publish bumps the build number, like every UI build.
        Write-Host "Publishing EliteFIPServer.UI (Release, x64)..." -ForegroundColor Cyan
        dotnet publish $uiProject -c Release -p:Platform=x64 --nologo -v:minimal
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet publish failed with exit code $LASTEXITCODE."
        }
    }

    # Package the build output, as Build-StorePackage.ps1 does, not the nested publish\ folder: that
    # folder leaves out the compiled XAML (*.xbf) and EliteFIPServer.pri, and the app then crashes at
    # startup in Microsoft.UI.Xaml.dll (0xc000027b). AppxManifest.xml and Package.appxmanifest are
    # Store-only packaging inputs.
    $contentDir = Join-Path $root "EliteFIPServer.UI\bin\x64\Release\net10.0-windows10.0.19041.0\win-x64"
    robocopy $contentDir $publishDir /E /XD (Join-Path $contentDir "publish") /XF AppxManifest.xml Package.appxmanifest *.pdb /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) {
        throw "Copying the build output failed (robocopy exit code $LASTEXITCODE)."
    }

    # Velopack wants a three-part SemVer version; the fourth part is always 0.
    $fullVersion = ([xml](Get-Content $versionPropsPath -Raw)).Project.PropertyGroup.Version
    $version = ($fullVersion -split '\.')[0..2] -join '.'
    Write-Host "Packaging version $version..." -ForegroundColor Cyan

    if ($DownloadPrevious) {
        dotnet vpk download github --repoUrl $repoUrl --outputDir $releasesDir
        if ($LASTEXITCODE -ne 0) {
            throw "vpk download failed with exit code $LASTEXITCODE."
        }
    }

    dotnet vpk pack `
        --packId $packId `
        --packVersion $version `
        --packDir $publishDir `
        --mainExe "EliteFIPServer.exe" `
        --runtime "win-x64" `
        --packTitle "Elite FIP Server" `
        --packAuthors "Earthstorm Software" `
        --icon $iconPath `
        --framework "net10.0-x64-runtime,net10.0-x64-aspnetcore" `
        --shortcuts "Desktop,StartMenuRoot" `
        --outputDir $releasesDir
    if ($LASTEXITCODE -ne 0) {
        throw "vpk pack failed with exit code $LASTEXITCODE."
    }

    if ($Publish) {
        if (-not $env:GITHUB_TOKEN) {
            throw "Set `$env:GITHUB_TOKEN to a token with contents:write on the repository before using -Publish."
        }

        dotnet vpk upload github --repoUrl $repoUrl --token $env:GITHUB_TOKEN --outputDir $releasesDir `
            --tag "v$version" --releaseName "Elite FIP Server $version"
        if ($LASTEXITCODE -ne 0) {
            throw "vpk upload failed with exit code $LASTEXITCODE."
        }
    }
}
finally {
    Pop-Location
}

$setup = Get-ChildItem $releasesDir -Filter "*Setup.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
Write-Host "Installer: $($setup.FullName) ($([math]::Round($setup.Length / 1MB, 1)) MB), version $version" -ForegroundColor Green
