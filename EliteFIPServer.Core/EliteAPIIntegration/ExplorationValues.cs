namespace EliteFIPServer;

// Approximate Universal Cartographics payouts, using the community-derived formulas (Odyssey rules).
// The game does not report values, so these are estimates for choosing which bodies are worth mapping.
public static class ExplorationValues
{
    private const double MassFactor = 0.56591828;
    private const double FirstDiscoveryMultiplier = 2.6;
    private const double EfficiencyMultiplier = 1.25;
    private const double MinimumBodyValue = 500;

    public static long EstimateStar(string starType, double stellarMass, bool firstDiscovery)
    {
        double k = starType switch
        {
            null or "" => 1200,
            "SupermassiveBlackHole" => 33.5678,
            "N" or "H" => 22628,
            _ when starType.StartsWith("D", StringComparison.Ordinal) => 14057,
            _ => 1200
        };

        double value = k + (stellarMass * k / 66.25);
        if (firstDiscovery)
        {
            value *= FirstDiscoveryMultiplier;
        }
        return (long)Math.Round(value);
    }

    public static long EstimatePlanet(string planetClass, string terraformState, double massEm,
        bool firstDiscovery, bool mapped, bool firstMapped, bool efficient)
    {
        bool terraformable = terraformState is "Terraformable" or "Terraforming";
        double k = planetClass switch
        {
            "Metal rich body" => 21790,
            "Ammonia world" => 96932,
            "Sudarsky class I gas giant" => 1656,
            "Sudarsky class II gas giant" => 9654,
            "High metal content body" => 9654 + (terraformable ? 100677 : 0),
            "Earthlike body" => 64831 + 116295,
            "Water world" => 64831 + (terraformable ? 116295 : 0),
            _ => 300 + (terraformable ? 93328 : 0)
        };

        double value = k + (k * MassFactor * Math.Pow(Math.Max(massEm, 0), 0.2));
        if (mapped)
        {
            double mapMultiplier = firstMapped && firstDiscovery ? 3.699622554
                : firstMapped ? 8.0956
                : 3.3333333333;
            if (efficient)
            {
                mapMultiplier *= EfficiencyMultiplier;
            }
            value *= mapMultiplier;
            value += Math.Max(value * 0.3, 555);
        }

        value = Math.Max(value, MinimumBodyValue);
        if (firstDiscovery)
        {
            value *= FirstDiscoveryMultiplier;
        }
        return (long)Math.Round(value);
    }
}
