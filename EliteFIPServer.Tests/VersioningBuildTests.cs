using System.Xml.Linq;
using Xunit;

namespace EliteFIPServer.Tests;

public class VersioningBuildTests
{
    [Fact]
    public void UI_project_should_auto_increment_version_before_build()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var uiProjectPath = Path.Combine(repoRoot, "EliteFIPServer.UI", "EliteFIPServer.UI.csproj");

        var projectXml = XDocument.Load(uiProjectPath);
        var bumpTarget = projectXml.Descendants("Target")
            .FirstOrDefault(target => target.ToString().Contains("UpdateBuildVersion.ps1", StringComparison.OrdinalIgnoreCase));

        // BeforeTargets="Build" runs after compilation, so the exe would carry the previous build's version.
        Assert.NotNull(bumpTarget);
        Assert.Equal("BeforeBuild", (string)bumpTarget!.Attribute("BeforeTargets"));
    }

    [Fact]
    public void Core_build_should_trigger_the_ui_build_when_core_builds()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var coreProjectPath = Path.Combine(repoRoot, "EliteFIPServer.Core", "EliteFIPServer.Core.csproj");
        var coreProjectXml = File.ReadAllText(coreProjectPath);

        Assert.Contains("MSBuild", coreProjectXml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EliteFIPServer.UI", coreProjectXml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BuildProjectReferences=false", coreProjectXml, StringComparison.OrdinalIgnoreCase);
    }
}
