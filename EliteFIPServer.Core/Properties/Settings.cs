using EliteFIPServer.Logging;
using Newtonsoft.Json;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace EliteFIPServer.Properties {

    // User settings, stored as JSON in %AppData%\EliteFIPServer\settings.json. This replaces the
    // ApplicationSettingsBase user.config, which lived in a folder keyed by app version and install
    // path, so every build and every Store update started again from defaults. In a Store install,
    // Windows redirects this path into the package's private storage, which survives updates and is
    // removed on uninstall.
    public sealed class Settings {

        public const int CurrentFormatVersion = 1;

        private static readonly Lazy<Settings> defaultInstance = new(() => Load(DefaultFilePath, DefaultLegacySettingsRoot));
        private readonly object saveLock = new();

        public static Settings Default => defaultInstance.Value;

        public static string DefaultFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EliteFIPServer", "settings.json");

        // Where ApplicationSettingsBase kept user.config files: <root>\<exe>_Url_<hash>\<version>\user.config.
        public static string DefaultLegacySettingsRoot => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EliteFIPServer");

        [JsonIgnore]
        public string FilePath { get; private set; }

        public int FormatVersion { get; set; } = CurrentFormatVersion;

        public bool EnableLog { get; set; }
        public bool AutostartPanelServer { get; set; }
        public bool PanelServerAllowLanAccess { get; set; }
        public string PanelServerAccessToken { get; set; } = "";
        public int PanelServerPort { get; set; } = 4545;
        // Serve files from the custom panels folder (PanelServer.CustomPanelsPath) ahead of the built-in pages.
        public bool UseCustomPanels { get; set; } = true;
        public int MatricApiPort { get; set; } = 5300;
        public bool AutostartMatricIntegration { get; set; }
        public int MatricRetryInterval { get; set; } = 30;
        public string MatricInstallPath { get; set; } = "";
        public bool DarkMode { get; set; }
        public int WindowWidth { get; set; } = 877;
        public int WindowHeight { get; set; } = 880;
        public int WindowLeft { get; set; } = -1;
        public int WindowTop { get; set; } = -1;
        public bool MinimiseToTray { get; set; }
        public bool StartMinimised { get; set; }
        public bool FirstRunCompleted { get; set; }

        public static Settings Load(string filePath, string legacySettingsRoot = null) {
            Settings settings = null;

            if (File.Exists(filePath)) {
                try {
                    settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(filePath));
                } catch (Exception ex) {
                    Log.Instance.Warn("Unable to read settings from {path}, using defaults: {error}", filePath, ex.Message);
                }
            } else if (legacySettingsRoot != null) {
                // First run with the JSON store: carry values over from the newest user.config.
                // They are written to the JSON file on the next Save.
                settings = ImportLegacyUserConfig(legacySettingsRoot);
            }

            settings ??= new Settings();
            settings.FilePath = filePath;
            settings.FormatVersion = CurrentFormatVersion;
            return settings;
        }

        public void Save() {
            lock (saveLock) {
                try {
                    Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                    // Write a temporary file and rename it, so an interrupted save can't leave a truncated file.
                    string tempPath = FilePath + ".tmp";
                    File.WriteAllText(tempPath, JsonConvert.SerializeObject(this, Formatting.Indented));
                    File.Move(tempPath, FilePath, true);
                } catch (Exception ex) {
                    Log.Instance.Warn("Unable to save settings to {path}: {error}", FilePath, ex.Message);
                }
            }
        }

        private static Settings ImportLegacyUserConfig(string legacySettingsRoot) {
            try {
                if (!Directory.Exists(legacySettingsRoot)) {
                    return null;
                }

                // Each version folder only holds the settings saved while that version ran, so apply
                // every file oldest first and let newer values win.
                var legacyFiles = new DirectoryInfo(legacySettingsRoot)
                    .EnumerateFiles("user.config", SearchOption.AllDirectories)
                    .OrderBy(file => file.LastWriteTimeUtc)
                    .ToList();
                if (legacyFiles.Count == 0) {
                    return null;
                }

                var settings = new Settings();
                foreach (FileInfo legacyFile in legacyFiles) {
                    try {
                        ApplyLegacyUserConfig(settings, XDocument.Load(legacyFile.FullName));
                    } catch (Exception ex) {
                        Log.Instance.Warn("Skipping unreadable settings file {path}: {error}", legacyFile.FullName, ex.Message);
                    }
                }

                Log.Instance.Info("Imported settings from {count} previous settings files", legacyFiles.Count);
                return settings;
            } catch (Exception ex) {
                Log.Instance.Warn("Unable to import previous settings: {error}", ex.Message);
                return null;
            }
        }

        private static void ApplyLegacyUserConfig(Settings settings, XDocument userConfig) {
            foreach (XElement setting in userConfig.Descendants("setting")) {
                var property = typeof(Settings).GetProperty((string)setting.Attribute("name") ?? "");
                string value = setting.Element("value")?.Value;
                if (property == null || !property.CanWrite || value == null) {
                    continue;
                }
                try {
                    property.SetValue(settings, Convert.ChangeType(value, property.PropertyType, CultureInfo.InvariantCulture));
                } catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException) {
                    // Keep the earlier or default value for a value that no longer parses.
                }
            }
        }
    }
}
