using Newtonsoft.Json;
using EliteFIPServer.Logging;

namespace EliteFIPServer;

public sealed class MatricButtonTextConfig
{
    public string ButtonName { get; set; }
    public string OffText { get; set; }
    public string OnText { get; set; }
    public bool UpdateButtonText { get; set; }
}

public static class MatricButtonTextConfigStore
{
    private static readonly IReadOnlyList<MatricButtonTextConfig> Defaults = new List<MatricButtonTextConfig>
    {
        new() { ButtonName = "LandingGear", OffText = "Landing Gear", OnText = "Landing Gear" },
        new() { ButtonName = "Supercruise", OffText = "Supercruise", OnText = "Supercruise" },
        new() { ButtonName = "FlightAssist", OffText = "Flight Assist", OnText = "Flight Assist" },
        new() { ButtonName = "Hardpoints", OffText = "Hardpoints", OnText = "Hardpoints" },
        new() { ButtonName = "Lights", OffText = "Lights", OnText = "Lights" },
        new() { ButtonName = "CargoScoop", OffText = "Cargo Scoop", OnText = "Cargo Scoop" },
        new() { ButtonName = "SilentRunning", OffText = "Silent Running", OnText = "Silent Running" },
        new() { ButtonName = "SrvHandbrake", OffText = "SRV Handbrake", OnText = "SRV Handbrake" },
        new() { ButtonName = "SrvTurret", OffText = "SRV Turret", OnText = "SRV Turret" },
        new() { ButtonName = "SrvDriveAssist", OffText = "SRV DriveAssist", OnText = "SRV DriveAssist" },
        new() { ButtonName = "HudMode", OffText = "Combat", OnText = "Analysis" },
        new() { ButtonName = "NightVision", OffText = "Night Vision", OnText = "Night Vision" },
        new() { ButtonName = "FsdJump", OffText = "FSD Jump", OnText = "FSD Jump" },
        new() { ButtonName = "SrvHighBeam", OffText = "SRV High Beam", OnText = "SRV High Beam" },
        new() { ButtonName = "AimDownSight", OffText = "Sights", OnText = "Sights" }
    };

    private static string ConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EliteFIPServer",
        "MatricButtonTextConfig.json");

    public static IReadOnlyList<MatricButtonTextConfig> Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var saved = JsonConvert.DeserializeObject<List<MatricButtonTextConfig>>(File.ReadAllText(ConfigPath));
                if (saved != null && saved.Count == Defaults.Count && saved.All(config => Defaults.Any(item => item.ButtonName == config.ButtonName)))
                {
                    return saved;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Info("Unable to load Matric button text config: {error}", ex.Message);
        }

        return CloneDefaults();
    }

    public static IReadOnlyList<MatricButtonTextConfig> GetDefaults() => CloneDefaults();

    public static void Save(IEnumerable<MatricButtonTextConfig> configs)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(configs, Formatting.Indented));
        }
        catch (Exception ex)
        {
            Log.Instance.Info("Unable to save Matric button text config: {error}", ex.Message);
        }
    }

    private static List<MatricButtonTextConfig> CloneDefaults() => Defaults
        .Select(config => new MatricButtonTextConfig
        {
            ButtonName = config.ButtonName,
            OffText = config.OffText,
            OnText = config.OnText,
            UpdateButtonText = config.UpdateButtonText
        })
        .ToList();
}
