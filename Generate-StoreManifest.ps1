# Merge local, git-ignored Microsoft Store identity values into a generated copy of
# Package.appxmanifest for MSIX packaging. The tracked Package.appxmanifest keeps safe
# placeholders and is never modified by this script.

param(
    [Parameter(Mandatory = $true)][string]$SourceManifestPath,
    [Parameter(Mandatory = $true)][string]$OutputManifestPath,
    [string]$VersionPropsPath,
    [Parameter(Mandatory = $true)][string]$IdentityJsonPath
)

if (-not (Test-Path $SourceManifestPath)) {
    throw "Source manifest not found: $SourceManifestPath"
}

if (-not (Test-Path $IdentityJsonPath)) {
    throw "Store identity file not found: $IdentityJsonPath`nCopy StoreIdentity.local.json.template to StoreIdentity.local.json (same folder) and fill in your real Partner Center values. That file is gitignored and must never be committed."
}

$identity = Get-Content $IdentityJsonPath -Raw | ConvertFrom-Json

foreach ($field in @('PackageIdentityName', 'Publisher', 'PublisherDisplayName')) {
    $value = $identity.$field
    if ([string]::IsNullOrWhiteSpace($value) -or $value -like 'REPLACE-WITH-*') {
        throw "StoreIdentity.local.json is missing a real value for '$field'. Fill in your Partner Center values before an MSIX build."
    }
}

$publisher = if ($identity.Publisher.StartsWith('CN=')) { $identity.Publisher } else { "CN=$($identity.Publisher)" }

$manifestContent = Get-Content $SourceManifestPath -Raw

# Scoped to just the <Identity ... /> element (not a global Name= replace) because "Name=" also
# appears on <TargetDeviceFamily> and the runFullTrust <Capability> — a prior version of this script
# used [regex]::Replace(...,...,1) intending "replace first match only", but PowerShell resolves the
# 4th positional int arg to the RegexOptions overload (1 = IgnoreCase), not a count, so it silently
# replaced every "Name=" in the document and corrupted those other elements.
$manifestContent = [regex]::Replace($manifestContent, '<Identity\s[\s\S]*?/>', {
    param($match)
    $block = $match.Value
    $block = [regex]::Replace($block, '(?<prefix>\sName=")[^"]*(?<suffix>")', ('${prefix}' + $identity.PackageIdentityName + '${suffix}'))
    $block = [regex]::Replace($block, '(?<prefix>\sPublisher=")[^"]*(?<suffix>")', ('${prefix}' + $publisher + '${suffix}'))
    return $block
})
$manifestContent = [regex]::Replace($manifestContent, '(?<prefix><PublisherDisplayName>)[^<]*(?<suffix></PublisherDisplayName>)', ('${prefix}' + $identity.PublisherDisplayName + '${suffix}'))

if ($VersionPropsPath -and (Test-Path $VersionPropsPath)) {
    $versionPropsContent = Get-Content $VersionPropsPath -Raw
    $versionMatch = [regex]::Match($versionPropsContent, '<AssemblyVersion>(\d+\.\d+\.\d+\.0)</AssemblyVersion>')
    if ($versionMatch.Success) {
        $manifestContent = [regex]::Replace($manifestContent, '(?<prefix><Identity\s[\s\S]*?\sVersion=")\d+\.\d+\.\d+\.\d+(?<suffix>")', ('${prefix}' + $versionMatch.Groups[1].Value + '${suffix}'))
    }
}

# $targetnametoken$/$targetentrypoint$ are normally substituted by the automated MSBuild packaging
# pipeline (which knows the build's actual output assembly name). For manual packaging (MakeAppx
# directly against a published output folder, bypassing that pipeline), substitute them here instead.
# Windows.FullTrustApplication is the standard EntryPoint sentinel for a converted Win32 desktop app
# (matches the runFullTrust capability this manifest declares) rather than a real UWP activation class.
$manifestContent = $manifestContent -replace '\$targetnametoken\$\.exe', 'EliteFIPServer.exe'
$manifestContent = $manifestContent -replace '\$targetentrypoint\$', 'Windows.FullTrustApplication'

$outputDir = Split-Path $OutputManifestPath -Parent
if ($outputDir -and -not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

Set-Content $OutputManifestPath $manifestContent
Write-Host "Generated Store manifest with local identity at $OutputManifestPath" -ForegroundColor Green
