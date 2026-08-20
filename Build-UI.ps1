param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$root = $PSScriptRoot
$versionScript = Join-Path $root "UpdateBuildVersion.ps1"
$versionProps = Join-Path $root "EliteFIPServer.Version.props"
$buildInfo = Join-Path $root "EliteFIPServer.UI\BuildInfo.cs"
$uiProject = Join-Path $root "EliteFIPServer.UI\EliteFIPServer.UI.csproj"

& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $versionScript `
    -VersionPropsPath $versionProps `
    -BuildInfoPath $buildInfo
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

dotnet build $uiProject --configuration $Configuration --no-restore
exit $LASTEXITCODE
