namespace EliteFIPServer;

// Engineering material grades, keyed by the journal's internal material name (language independent), and the
// storage limit for each grade. Materials missing from the table (Guardian, Thargoid and newer additions)
// report grade 0 and no limit.
public static class MaterialGrades
{
    private static readonly long[] Maximums = { 0, 300, 250, 200, 150, 100 };

    private static readonly Dictionary<string, int> Grades = BuildGrades();

    private static Dictionary<string, int> BuildGrades()
    {
        var grades = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        void Add(int grade, params string[] names) { foreach (var name in names) grades[name] = grade; }

        // Raw
        Add(1, "carbon", "iron", "lead", "nickel", "phosphorus", "rhenium", "sulphur");
        Add(2, "arsenic", "chromium", "germanium", "manganese", "vanadium", "zinc", "zirconium");
        Add(3, "boron", "cadmium", "mercury", "molybdenum", "niobium", "tin", "tungsten");
        Add(4, "antimony", "polonium", "ruthenium", "selenium", "technetium", "tellurium", "yttrium");

        // Manufactured, one family per line from grade 1 to 5
        Add(1, "basicconductors", "chemicalstorageunits", "compactcomposites", "crystalshards", "gridresistors",
            "heatconductionwiring", "mechanicalscrap", "salvagedalloys", "wornshieldemitters", "temperedalloys");
        Add(2, "conductivecomponents", "chemicalprocessors", "filamentcomposites", "uncutfocuscrystals", "hybridcapacitors",
            "heatdispersionplate", "mechanicalequipment", "galvanisingalloys", "shieldemitters", "heatresistantceramics");
        Add(3, "conductiveceramics", "chemicaldistillery", "highdensitycomposites", "focuscrystals", "electrochemicalarrays",
            "heatexchangers", "mechanicalcomponents", "phasealloys", "shieldingsensors", "precipitatedalloys");
        Add(4, "conductivepolymers", "chemicalmanipulators", "fedproprietarycomposites", "refinedfocuscrystals", "polymercapacitors",
            "heatvanes", "configurablecomponents", "protolightalloys", "compoundshielding", "thermicalloys");
        Add(5, "biotechconductors", "pharmaceuticalisolators", "fedcorecomposites", "exquisitefocuscrystals", "militarysupercapacitors",
            "protoheatradiators", "improvisedcomponents", "protoradiolicalloys", "imperialshielding", "militarygradealloys");

        // Encoded
        Add(1, "bulkscandata", "disruptedwakeechoes", "legacyfirmware", "shieldcyclerecordings", "encryptedfiles", "scrambledemissiondata");
        Add(2, "scanarchives", "fsdtelemetry", "consumerfirmware", "shieldsoakanalysis", "encryptioncodes", "archivedemissiondata");
        Add(3, "scandatabanks", "wakesolutions", "industrialfirmware", "shielddensityreports", "symmetrickeys", "emissiondata");
        Add(4, "encodedscandata", "hyperspacetrajectories", "securityfirmware", "shieldpatternanalysis", "encryptionarchives", "decodedemissiondata");
        Add(5, "classifiedscandata", "dataminedwake", "embeddedfirmware", "shieldfrequencydata", "adaptiveencryptors", "compactemissionsdata");
        return grades;
    }

    public static int Grade(string symbol) =>
        !string.IsNullOrWhiteSpace(symbol) && Grades.TryGetValue(symbol.Trim('$', ';').Replace("_name", "", StringComparison.OrdinalIgnoreCase), out int grade) ? grade : 0;

    public static long Maximum(int grade) => grade >= 1 && grade < Maximums.Length ? Maximums[grade] : 0;
}
