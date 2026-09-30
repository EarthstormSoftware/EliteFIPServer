param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

# The UI project's UpdateBuildVersion target increments the version once per build, so no
# separate version step is needed here.
$uiProject = Join-Path $PSScriptRoot "EliteFIPServer.UI\EliteFIPServer.UI.csproj"

dotnet build $uiProject --configuration $Configuration --no-restore
exit $LASTEXITCODE
