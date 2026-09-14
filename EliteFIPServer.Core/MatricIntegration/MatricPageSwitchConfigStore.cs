using EliteFIPServer.Logging;
using Newtonsoft.Json;

namespace EliteFIPServer;

public sealed class MatricPageSwitchConfig
{
    public string State { get; set; }
    public bool Enabled { get; set; }
    public string PageId { get; set; }
}

public static class MatricPageSwitchConfigStore
{
    private static readonly IReadOnlyList<MatricPageSwitchConfig> Defaults = new List<MatricPageSwitchConfig>
    {
        new() { State = "InMainShip", PageId = "" },
        new() { State = "InFighter", PageId = "" },
        new() { State = "InSRV", PageId = "" },
        new() { State = "OnFoot", PageId = "" },
        new() { State = "HardpointsDeployed", PageId = "" },
        new() { State = "HardpointsRetracted", PageId = "" }
    };

    private static string ConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EliteFIPServer",
        "MatricPageSwitchConfig.json");

    public static IReadOnlyList<MatricPageSwitchConfig> Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var saved = JsonConvert.DeserializeObject<List<MatricPageSwitchConfig>>(File.ReadAllText(ConfigPath));
                if (IsValid(saved))
                {
                    return MergeMissingDefaults(saved);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Info("Unable to load Matric page switch config: {error}", ex.Message);
        }

        return CloneDefaults();
    }

    public static IReadOnlyList<MatricPageSwitchConfig> GetDefaults() => CloneDefaults();

    public static void Save(IEnumerable<MatricPageSwitchConfig> configs)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(configs, Formatting.Indented));
        }
        catch (Exception ex)
        {
            Log.Instance.Info("Unable to save Matric page switch config: {error}", ex.Message);
        }
    }

    private static bool IsValid(IReadOnlyCollection<MatricPageSwitchConfig> configs) =>
        configs != null &&
        configs.Count > 0 &&
        configs.All(config => !string.IsNullOrWhiteSpace(config?.State)) &&
        configs.Select(config => config.State).Distinct().Count() == configs.Count;

    private static List<MatricPageSwitchConfig> MergeMissingDefaults(List<MatricPageSwitchConfig> saved)
    {
        var missing = Defaults.Where(defaultConfig => saved.All(config => config.State != defaultConfig.State));
        saved.AddRange(missing.Select(defaultConfig => new MatricPageSwitchConfig
        {
            State = defaultConfig.State,
            Enabled = defaultConfig.Enabled,
            PageId = defaultConfig.PageId
        }));
        return saved;
    }

    private static List<MatricPageSwitchConfig> CloneDefaults() => Defaults
        .Select(config => new MatricPageSwitchConfig
        {
            State = config.State,
            Enabled = config.Enabled,
            PageId = config.PageId
        })
        .ToList();
}
