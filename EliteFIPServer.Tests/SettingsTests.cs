using EliteFIPServer.Properties;
using Xunit;

namespace EliteFIPServer.Tests;

public class SettingsTests : IDisposable
{
    private readonly string tempRoot = Path.Combine(Path.GetTempPath(), "EliteFIPServerSettingsTests", Guid.NewGuid().ToString("N"));

    private string SettingsPath => Path.Combine(tempRoot, "roaming", "settings.json");
    private string LegacyRoot => Path.Combine(tempRoot, "local");

    public void Dispose()
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void Load_should_return_defaults_when_no_settings_exist()
    {
        var settings = Settings.Load(SettingsPath, LegacyRoot);

        Assert.Equal(4545, settings.PanelServerPort);
        Assert.Equal(5300, settings.MatricApiPort);
        Assert.Equal(30, settings.MatricRetryInterval);
        Assert.Equal(-1, settings.WindowLeft);
        Assert.False(settings.EnableLog);
        Assert.False(File.Exists(SettingsPath));
    }

    [Fact]
    public void Saved_settings_should_load_back_unchanged()
    {
        var settings = Settings.Load(SettingsPath, LegacyRoot);
        settings.PanelServerPort = 4600;
        settings.PanelServerAllowLanAccess = true;
        settings.MatricInstallPath = @"D:\Matric";
        settings.Save();

        var reloaded = Settings.Load(SettingsPath, LegacyRoot);

        Assert.Equal(4600, reloaded.PanelServerPort);
        Assert.True(reloaded.PanelServerAllowLanAccess);
        Assert.Equal(@"D:\Matric", reloaded.MatricInstallPath);
        Assert.False(File.Exists(SettingsPath + ".tmp"));
    }

    [Fact]
    public void Load_should_import_the_newest_legacy_user_config()
    {
        WriteUserConfig("4.0.50.0", DateTime.UtcNow.AddDays(-2), port: "4000", lan: "False");
        WriteUserConfig("4.0.53.0", DateTime.UtcNow.AddDays(-1), port: "4700", lan: "True");

        var settings = Settings.Load(SettingsPath, LegacyRoot);

        Assert.Equal(4700, settings.PanelServerPort);
        Assert.True(settings.PanelServerAllowLanAccess);
        Assert.True(settings.FirstRunCompleted);
        Assert.Equal(5300, settings.MatricApiPort);
    }

    [Fact]
    public void Load_should_keep_legacy_values_that_only_older_versions_saved()
    {
        WriteUserConfig("4.0.44.0", DateTime.UtcNow.AddDays(-5), port: "4000", lan: "False", darkMode: "True");
        WriteUserConfig("4.0.53.0", DateTime.UtcNow.AddDays(-1), port: "4700", lan: "True");

        var settings = Settings.Load(SettingsPath, LegacyRoot);

        Assert.True(settings.DarkMode);
        Assert.Equal(4700, settings.PanelServerPort);
    }

    [Fact]
    public void Load_should_prefer_the_json_file_over_legacy_settings()
    {
        WriteUserConfig("4.0.53.0", DateTime.UtcNow, port: "4700", lan: "True");
        var settings = Settings.Load(SettingsPath, LegacyRoot);
        settings.PanelServerPort = 4800;
        settings.Save();

        var reloaded = Settings.Load(SettingsPath, LegacyRoot);

        Assert.Equal(4800, reloaded.PanelServerPort);
    }

    [Fact]
    public void Load_should_fall_back_to_defaults_when_the_file_is_corrupt()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, "{ not json");

        var settings = Settings.Load(SettingsPath, LegacyRoot);

        Assert.Equal(4545, settings.PanelServerPort);
    }

    private void WriteUserConfig(string version, DateTime lastWriteUtc, string port, string lan, string darkMode = null)
    {
        var darkModeSetting = darkMode == null ? "" : $"""<setting name="DarkMode" serializeAs="String"><value>{darkMode}</value></setting>""";
        var directory = Path.Combine(LegacyRoot, "EliteFIPServer_Url_testhash", version);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "user.config");
        File.WriteAllText(path, $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <userSettings>
                <EliteFIPServer.Properties.Settings>
                  <setting name="PanelServerPort" serializeAs="String"><value>{port}</value></setting>
                  <setting name="PanelServerAllowLanAccess" serializeAs="String"><value>{lan}</value></setting>
                  <setting name="FirstRunCompleted" serializeAs="String"><value>True</value></setting>
                  <setting name="MatricApiPort" serializeAs="String"><value>not a number</value></setting>
                  <setting name="RemovedSetting" serializeAs="String"><value>x</value></setting>
                  {darkModeSetting}
                </EliteFIPServer.Properties.Settings>
              </userSettings>
            </configuration>
            """);
        File.SetLastWriteTimeUtc(path, lastWriteUtc);
    }
}
