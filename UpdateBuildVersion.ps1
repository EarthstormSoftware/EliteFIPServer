# Auto-increment the UI build version and diagnostic build string.
# External format: Major.Minor.Build.0
# Internal format: Major.Minor.YYYYMMDD_BBBBB

param(
    [string]$VersionPropsPath,
    [string]$BuildInfoPath,
    [string]$ProjectPath,
    [string]$VersionOutputPath,
    [string]$AppxManifestPath
)

if (-not $VersionPropsPath -or -not $BuildInfoPath) {
    exit 0
}

# Write via a temp file + rename so an interrupted build can never leave a truncated or
# NUL-filled version file behind (EliteFIPServer.Version.props was once corrupted that way,
# which breaks every subsequent MSBuild evaluation). WriteAllText also avoids Set-Content's
# ANSI default encoding and the extra trailing newline it appends on every run.
function Write-FileAtomic([string]$Path, [string]$Content) {
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $tempPath = "$fullPath.tmp"
    [System.IO.File]::WriteAllText($tempPath, $Content, (New-Object System.Text.UTF8Encoding $false))
    Move-Item -LiteralPath $tempPath -Destination $fullPath -Force
}

$today = [DateTime]::Now.ToString("yyyyMMdd")

Write-Host "Build Version Update Script" -ForegroundColor Cyan
Write-Host "Today's date code: $today" -ForegroundColor Green

# Read with File.ReadAllText (BOM-aware, UTF-8 default), not Get-Content: Windows PowerShell 5.1
# reads BOM-less UTF-8 as ANSI, so non-ASCII text (e.g. em dashes in the manifest's comments)
# would be re-encoded as mojibake on every build once written back as UTF-8.
function Read-FileText([string]$Path) {
    return [System.IO.File]::ReadAllText([System.IO.Path]::GetFullPath($Path))
}

$versionPropsContent = Read-FileText $VersionPropsPath
$buildInfoContent = Read-FileText $BuildInfoPath

$versionMatch = [regex]::Match($versionPropsContent, '<AssemblyVersion>(\d+)\.(\d+)\.(\d+)\.0</AssemblyVersion>')

if ($versionMatch.Success) {
    $major = $versionMatch.Groups[1].Value
    $minor = $versionMatch.Groups[2].Value
    $currentBuild = [int]$versionMatch.Groups[3].Value
    $newBuild = $currentBuild + 1

    if ($newBuild -gt 65535) {
        throw "Build number limit reached for version $major.$minor. Start a new minor release."
    }
    
    $newVersion = "$major.$minor.$newBuild.0"
    $newBuildString = "$major.$minor.$($today)_$($newBuild.ToString('D5'))"
    Write-Host "New version: $newVersion" -ForegroundColor Cyan
    
    $versionPropsContent = $versionPropsContent -replace '<AssemblyVersion>\d+\.\d+\.\d+\.0</AssemblyVersion>', "<AssemblyVersion>$newVersion</AssemblyVersion>"
    $versionPropsContent = $versionPropsContent -replace '<FileVersion>\d+\.\d+\.\d+\.0</FileVersion>', "<FileVersion>$newVersion</FileVersion>"
    $versionPropsContent = $versionPropsContent -replace '<Version>\d+\.\d+\.\d+\.0</Version>', "<Version>$newVersion</Version>"
    $buildInfoContent = $buildInfoContent -replace 'public const string BuildString = "[^"]+";', ('public const string BuildString = "' + $newBuildString + '";')
    $buildInfoContent = $buildInfoContent -replace 'public const string Version = "[^"]+";', ('public const string Version = "' + $newVersion + '";')

    Write-FileAtomic $VersionPropsPath $versionPropsContent
    Write-FileAtomic $BuildInfoPath $buildInfoContent
    if ($VersionOutputPath) {
        Write-FileAtomic $VersionOutputPath $newVersion
    }

    # Store submissions require the MSIX identity version to match, with a revision of 0.
    if ($AppxManifestPath -and (Test-Path $AppxManifestPath)) {
        $manifestContent = Read-FileText $AppxManifestPath
        $updatedManifest = [regex]::Replace($manifestContent, '(?<prefix>\sVersion=")\d+\.\d+\.\d+\.\d+(?<suffix>")', ('${prefix}' + $newVersion + '${suffix}'), 1)
        if ($updatedManifest -ne $manifestContent) {
            Write-FileAtomic $AppxManifestPath $updatedManifest
            Write-Host "  Package.appxmanifest: $newVersion" -ForegroundColor Green
        }
        else {
            Write-Host "WARNING: Could not update Identity Version in $AppxManifestPath" -ForegroundColor Yellow
        }
    }
    
    Write-Host "Version updated successfully!" -ForegroundColor Green
    Write-Host "  AssemblyVersion: $newVersion" -ForegroundColor Green
    Write-Host "  FileVersion: $newVersion" -ForegroundColor Green
    Write-Host "  BuildString: $newBuildString" -ForegroundColor Green
}
else {
    Write-Host "ERROR: Could not find <AssemblyVersion>Major.Minor.Build.0</AssemblyVersion> in $VersionPropsPath (file missing or corrupted?)" -ForegroundColor Red
    exit 1
}
