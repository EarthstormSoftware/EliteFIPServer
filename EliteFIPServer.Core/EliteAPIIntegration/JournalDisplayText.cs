using System.Text.RegularExpressions;

namespace EliteFIPServer;

internal static class JournalDisplayText
{
    private static readonly Dictionary<string, string> KnownLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Agri"] = "Agriculture",
        ["apexinterstellar"] = "Apex Interstellar",
        ["autodock"] = "Auto Dock",
        ["bartender"] = "Bartender",
        ["commodities"] = "Commodities Market",
        ["crewlounge"] = "Crew Lounge",
        ["crimescanner"] = "Kill Warrant Scanner",
        ["dock"] = "Docking",
        ["engineer"] = "Engineer Workshop",
        ["Extraction"] = "Extraction",
        ["exploration"] = "Universal Cartographics",
        ["flightcontroller"] = "Flight Controller",
        ["frontlinesolutions"] = "Frontline Solutions",
        ["gimbal"] = "Gimballed",
        ["HighTech"] = "High Tech",
        ["hyperdrive"] = "Frame Shift Drive",
        ["Industrial"] = "Industrial",
        ["livery"] = "Livery",
        ["Military"] = "Military",
        ["missions"] = "Mission Board",
        ["missionsgenerated"] = "Mission Board",
        ["multicannon"] = "Multi-Cannon",
        ["outfitting"] = "Outfitting",
        ["pioneersupplies"] = "Pioneer Supplies",
        ["plasmapointdefence"] = "Point Defence",
        ["powerplay"] = "Power Contact",
        ["pulselaser"] = "Pulse Laser",
        ["rearm"] = "Restock",
        ["Refinery"] = "Refinery",
        ["registeringcolonisation"] = "Colonisation",
        ["searchrescue"] = "Search and Rescue",
        ["Service"] = "Service",
        ["shipyard"] = "Shipyard",
        ["shieldbooster"] = "Shield Booster",
        ["shop"] = "Shop",
        ["socialspace"] = "Social Space",
        ["stationoperations"] = "Station Operations",
        ["stationMenu"] = "Station Services",
        ["Terraforming"] = "Terraforming",
        ["Tourism"] = "Tourism",
        ["tuning"] = "Engineer Workshop",
        ["vistagenomics"] = "Vista Genomics"
    };

    public static string Format(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var text = value.Trim();
        var isSymbol = text.StartsWith('$') && text.EndsWith(';');
        if (isSymbol) text = text[1..^1];

        var parts = text.Split('_', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (parts.Count > 1 && IsTechnicalPrefix(parts[0])) parts.RemoveAt(0);
        if (parts.Count == 0) return value;

        if (parts.Count == 1 && KnownLabels.TryGetValue(parts[0], out var known)) return known;

        var words = parts.SelectMany(SplitWords).ToList();
        if (isSymbol && words.Count == 2 && words[0].Equals("Security", StringComparison.OrdinalIgnoreCase)) {
            words.Reverse();
        }
        if (text.StartsWith("hpt_", StringComparison.OrdinalIgnoreCase) && words.Count > 1 && IsModuleSize(words[^1])) {
            var size = words[^1];
            words.RemoveAt(words.Count - 1);
            words.Insert(0, size);
        }

        return string.Join(" ", words.Select(FormatWord));
    }

    private static bool IsTechnicalPrefix(string value) =>
        value.Equals("economy", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("system", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("mission", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("weapon", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("hpt", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("int", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("government", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("happiness", StringComparison.OrdinalIgnoreCase);

    private static bool IsModuleSize(string value) =>
        value.Equals("Tiny", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("Small", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("Medium", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("Large", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("Huge", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> SplitWords(string value) =>
        Regex.Matches(value, @"[A-Z]+(?=[A-Z][a-z]|\b)|[A-Z]?[a-z]+|\d+")
            .Select(match => match.Value);

    private static string FormatWord(string value)
    {
        if (KnownLabels.TryGetValue(value, out var known)) return known;
        if (value.Length <= 3 && value.All(char.IsUpper)) return value;
        return char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
    }
}