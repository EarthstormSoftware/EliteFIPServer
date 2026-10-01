using EliteAPI.Events.Game;
using EliteAPI.Journals;
using EliteAPI.Json;
using Newtonsoft.Json;
using Xunit;

namespace EliteFIPServer.Tests;

public class ExobiologyAndRouteTests
{
    private static EliteAPIIntegration StartedIntegration()
    {
        var server = new CoreServer(Array.Empty<string>());
        server.EliteAPIIntegration.CurrentState.Set(RunState.Started);
        return server.EliteAPIIntegration;
    }

    // Journal lines carry symbols plus *_Localised names; EliteAPI resolves them through its localisation table.
    private static T JournalEvent<T>(string json)
    {
        JournalUtils.PrepareLocalisations(json);
        return JsonConvert.DeserializeObject<T>(json, JsonUtils.SerializerSettings);
    }

    private static ScanOrganicEvent OrganicScan(string scanType, string species = "Stratum_07", string speciesName = "Stratum Tectonicas", long body = 12) =>
        JournalEvent<ScanOrganicEvent>($$"""
            { "timestamp":"2026-10-01T12:00:00Z", "event":"ScanOrganic", "ScanType":"{{scanType}}",
              "Genus":"$Codex_Ent_Stratum_Genus_Name;", "Genus_Localised":"Stratum",
              "Species":"$Codex_Ent_{{species}}_Name;", "Species_Localised":"{{speciesName}}",
              "Variant":"$Codex_Ent_{{species}}_A_Name;", "Variant_Localised":"{{speciesName}} - Green",
              "SystemAddress":100, "Body":{{body}} }
            """);

    private static void StandOn(EliteAPIIntegration integration, double latitude, double longitude) =>
        integration.HandleStatusEvent(("Status", FormattableString.Invariant($$"""
            { "timestamp":"2026-10-01T12:00:00Z", "event":"Status", "Flags":2097152, "Flags2":16,
              "Latitude":{{latitude}}, "Longitude":{{longitude}}, "Heading":0, "Altitude":0,
              "BodyName":"Test 1 a", "PlanetRadius":2000000,
              "Destination":{ "System":0, "Body":0, "Name":"" } }
            """)));

    [Fact]
    public void Organic_scans_track_samples_positions_and_value()
    {
        var integration = StartedIntegration();

        StandOn(integration, 10, 20);
        integration.HandleScanOrganicEvent(OrganicScan("Log"));
        StandOn(integration, 10.01, 20);
        integration.HandleScanOrganicEvent(OrganicScan("Sample"));

        var current = integration.CurrentExobiology.Current;
        Assert.NotNull(current);
        Assert.Equal("Stratum Tectonicas", current.Species);
        Assert.Equal("Stratum", current.Genus);
        Assert.Equal(2, current.SamplesTaken);
        Assert.Equal(500, current.SampleDistance);
        Assert.Equal(19010800, current.EstimatedValue);
        Assert.Equal(new[] { 10, 10.01 }, current.Samples.Select(sample => sample.Latitude));
        Assert.All(current.Samples, sample => Assert.Equal(2000000, sample.PlanetRadius));

        integration.HandleScanOrganicEvent(OrganicScan("Sample"));
        integration.HandleScanOrganicEvent(OrganicScan("Analyse"));

        var exobiology = integration.CurrentExobiology;
        Assert.Null(exobiology.Current);
        var analysed = Assert.Single(exobiology.Unsold);
        Assert.Equal(3, analysed.SamplesTaken);
        Assert.Equal(19010800, exobiology.UnsoldEstimatedValue);
    }

    [Fact]
    public void Logging_another_species_abandons_the_one_in_progress()
    {
        var integration = StartedIntegration();

        integration.HandleScanOrganicEvent(OrganicScan("Log"));
        integration.HandleScanOrganicEvent(OrganicScan("Log", "Bacterial_04", "Bacterium Informem"));

        var current = integration.CurrentExobiology.Current;
        Assert.Equal("Bacterium Informem", current.Species);
        Assert.Equal(1, current.SamplesTaken);
        Assert.Empty(integration.CurrentExobiology.Unsold);
    }

    [Fact]
    public void Selling_removes_sold_species_and_death_loses_the_rest()
    {
        var integration = StartedIntegration();
        foreach (var species in new[] { ("Stratum_07", "Stratum Tectonicas"), ("Bacterial_04", "Bacterium Informem") })
        {
            integration.HandleScanOrganicEvent(OrganicScan("Log", species.Item1, species.Item2));
            integration.HandleScanOrganicEvent(OrganicScan("Analyse", species.Item1, species.Item2));
        }

        integration.HandleSellOrganicDataEvent(JournalEvent<SellOrganicDataEvent>("""
            { "timestamp":"2026-10-01T13:00:00Z", "event":"SellOrganicData", "MarketID":1,
              "BioData":[ { "Genus":"$Codex_Ent_Stratum_Genus_Name;", "Genus_Localised":"Stratum",
                "Species":"$Codex_Ent_Stratum_07_Name;", "Species_Localised":"Stratum Tectonicas",
                "Variant":"$Codex_Ent_Stratum_07_A_Name;", "Variant_Localised":"Stratum Tectonicas - Green",
                "Value":19010800, "Bonus":0 } ] }
            """));

        var remaining = Assert.Single(integration.CurrentExobiology.Unsold);
        Assert.Equal("Bacterium Informem", remaining.Species);
        Assert.Equal(19010800, integration.CurrentExobiology.LastSaleValue);

        integration.HandleDiedEvent(new DiedEvent { Timestamp = new DateTime(2026, 10, 1, 14, 0, 0, DateTimeKind.Utc) });
        Assert.Empty(integration.CurrentExobiology.Unsold);
    }

    [Theory]
    [InlineData("Stratum Tectonicas", 19010800)]
    [InlineData("Roseum Brain Tree", 1593700)]
    [InlineData("Unknown Species", 0)]
    public void Species_values_cover_named_species_and_colour_families(string species, long expected)
    {
        Assert.Equal(expected, ExobiologyValues.EstimateSpecies(species));
    }

    [Fact]
    public void Exobiology_snapshot_is_detached_from_the_source()
    {
        var samples = new List<BioSamplePositionData> { new() { Latitude = 1 } };
        var source = new ExobiologyData { Current = new BioScanData { Species = "Osseus Discus", Samples = samples } };

        var clone = (ExobiologyData)GameDataSnapshot.Clone(GameEventType.Exobiology, source);
        samples.Add(new BioSamplePositionData());

        Assert.Equal("Osseus Discus", clone.Current.Species);
        Assert.Single(clone.Current.Samples);
    }

    [Fact]
    public void Route_jump_distance_is_straight_line_light_years()
    {
        Assert.Equal(5, EliteAPIIntegration.DistanceBetween(new double[] { 0, 0, 0 }, new double[] { 3, 4, 0 }));
        Assert.Equal(0, EliteAPIIntegration.DistanceBetween(null, new double[] { 3, 4, 0 }));
        Assert.Equal(0, EliteAPIIntegration.DistanceBetween(Array.Empty<double>(), new double[] { 3, 4, 0 }));
    }
}
