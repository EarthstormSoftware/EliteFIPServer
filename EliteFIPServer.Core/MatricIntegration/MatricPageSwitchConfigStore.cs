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
        new() { State = "OnFoot", PageId = "" }
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
                    return saved;
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
        configs.Count == Defaults.Count &&
        Defaults.All(defaultConfig => configs.Count(config => config?.State == defaultConfig.State) == 1);

    private static List<MatricPageSwitchConfig> CloneDefaults() => Defaults
        .Select(config => new MatricPageSwitchConfig
        {
            State = config.State,
            Enabled = config.Enabled,
            PageId = config.PageId
        })
        .ToList();
}
