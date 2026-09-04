using EliteFIPServer.Logging;
using Newtonsoft.Json;

namespace EliteFIPServer;

public sealed class MatricClientProfile
{
    public string ClientId { get; set; }
    public string ClientName { get; set; }
    public string DeckId { get; set; }
    public List<MatricPageSwitchConfig> PageSwitches { get; set; } = new();
}

public static class MatricClientProfileStore
{
    private static string ConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EliteFIPServer",
        "MatricClientProfiles.json");

    public static IReadOnlyList<MatricClientProfile> Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var saved = JsonConvert.DeserializeObject<List<MatricClientProfile>>(File.ReadAllText(ConfigPath));
                if (saved != null)
                {
                    return saved;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Info("Unable to load Matric client profiles: {error}", ex.Message);
        }

        return Array.Empty<MatricClientProfile>();
    }

    public static void Save(IEnumerable<MatricClientProfile> profiles)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(profiles, Formatting.Indented));
        }
        catch (Exception ex)
        {
            Log.Instance.Info("Unable to save Matric client profiles: {error}", ex.Message);
        }
    }
}
