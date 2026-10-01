using EliteAPI.Events.Game;
using Xunit;

namespace EliteFIPServer.Tests;

public class ExplorationAndMissionTrackingTests
{
    private static EliteAPIIntegration StartedIntegration()
    {
        var server = new CoreServer(Array.Empty<string>());
        server.EliteAPIIntegration.CurrentState.Set(RunState.Started);
        return server.EliteAPIIntegration;
    }

    [Fact]
    public void System_exploration_collects_scans_mapping_and_discovery_progress()
    {
        var integration = StartedIntegration();
        var time = new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);

        integration.HandleLocationEvent(new LocationEvent { Timestamp = time, SystemAddress = "100", StarSystem = "Test System" });
        integration.HandleFssDiscoveryScanEvent(new FssDiscoveryScanEvent
        {
            Timestamp = time, SystemAddress = "100", SystemName = "Test System", BodyCount = 3, NonBodyCount = 2, Progress = 0.4
        });
        integration.HandleScanEvent(new ScanEvent
        {
            Timestamp = time, SystemAddress = "100", StarSystem = "Test System", BodyId = "0", BodyName = "Test System A",
            StarType = "K", StellarMass = 0.7, WasDiscovered = true
        });
        integration.HandleScanEvent(new ScanEvent
        {
            Timestamp = time, SystemAddress = "100", StarSystem = "Test System", BodyId = "3", BodyName = "Test System 1",
            PlanetClass = "Earthlike body", TerraformState = "Terraformed", MassEm = 1, DistanceFromArrivalLs = 450
        });
        // A belt cluster has neither a star type nor a planet class and is not listed.
        integration.HandleScanEvent(new ScanEvent
        {
            Timestamp = time, SystemAddress = "100", StarSystem = "Test System", BodyId = "5", BodyName = "Test System A Belt Cluster 1"
        });
        integration.HandleSaaScanCompleteEvent(new SaaScanCompleteEvent
        {
            Timestamp = time, SystemAddress = "100", BodyId = "3", BodyName = "Test System 1", ProbesUsed = 5, EfficiencyTarget = 7
        });
        integration.HandleFssAllBodiesFoundEvent(new FssAllBodiesFoundEvent
        {
            Timestamp = time, SystemAddress = "100", SystemName = "Test System", Count = 3
        });

        var exploration = integration.CurrentSystemExploration;
        Assert.Equal("Test System", exploration.SystemName);
        Assert.Equal(3, exploration.BodyCount);
        Assert.Equal(2, exploration.NonBodyCount);
        Assert.True(exploration.AllBodiesFound);
        Assert.Equal(new[] { "Test System A", "Test System 1" }, exploration.Bodies.Select(body => body.BodyName));

        var earthlike = exploration.Bodies[1];
        Assert.Equal("Planet", earthlike.BodyType);
        Assert.True(earthlike.IsMapped);
        Assert.True(earthlike.MappedEfficiently);
        Assert.Equal(earthlike.EstimatedMappedValue, earthlike.EstimatedValue);
        Assert.Equal(exploration.Bodies.Sum(body => body.EstimatedValue), exploration.EstimatedValue);
        Assert.Equal(0, exploration.Bodies[0].EstimatedMappedValue);
    }

    [Fact]
    public void System_exploration_starts_again_in_a_new_system()
    {
        var integration = StartedIntegration();
        integration.HandleScanEvent(new ScanEvent { SystemAddress = "100", StarSystem = "First", BodyId = "0", BodyName = "First A", StarType = "G" });
        integration.HandleFsdJumpEvent(new FsdJumpEvent { SystemAddress = "200", StarSystem = "Second" });

        Assert.Equal("Second", integration.CurrentSystemExploration.SystemName);
        Assert.Empty(integration.CurrentSystemExploration.Bodies);
        Assert.Equal(0, integration.CurrentSystemExploration.BodyCount);
    }

    [Fact]
    public void Docking_sets_the_exploration_system_when_no_location_event_was_seen()
    {
        var integration = StartedIntegration();
        integration.HandleDockedEvent(new DockedEvent { Timestamp = DateTime.UtcNow, SystemAddress = "300", StarSystem = "Candecama" });

        Assert.Equal("Candecama", integration.CurrentSystemExploration.SystemName);
        Assert.True(integration.CurrentSystemExploration.LastUpdate > DateTime.MinValue);
    }

    [Fact]
    public void Active_missions_follow_accepted_redirected_and_completed_events()
    {
        var integration = StartedIntegration();
        var time = new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);

        integration.HandleMissionsEvent(new MissionsEvent
        {
            Timestamp = time,
            Active = new[] { new MissionsEvent.MissionInfo { MissionId = "1", Name = "Mission_Courier_name", Expires = 3600 } }
        });
        integration.HandleMissionAcceptedEvent(new MissionAcceptedEvent
        {
            Timestamp = time, MissionId = "2", Name = "Mission_Delivery", LocalisedName = "Deliver 10 units of Gold",
            Faction = "Test Faction", DestinationSystem = "Lave", DestinationStation = "Lave Station",
            Expiry = time.AddHours(5), Reward = 250000, Count = 10, Commodity = "Gold"
        });
        integration.HandleMissionRedirectedEvent(new MissionRedirectedEvent
        {
            Timestamp = time, MissionId = "2", NewDestinationSystem = "Leesti", NewDestinationStation = "George Lucas"
        });

        var active = integration.CurrentMissions.Active;
        Assert.Equal(new[] { "1", "2" }, active.Select(mission => mission.MissionId));
        Assert.Equal(time.AddHours(1), active[0].Expiry);
        Assert.Equal("Deliver 10 units of Gold", active[1].Name);
        Assert.Equal("Leesti", active[1].DestinationSystem);
        Assert.Equal("George Lucas", active[1].DestinationStation);
        Assert.Equal(250000, active[1].Reward);

        // A later Missions list keeps the details already known for a mission.
        integration.HandleMissionsEvent(new MissionsEvent
        {
            Timestamp = time.AddMinutes(10),
            Active = new[] { new MissionsEvent.MissionInfo { MissionId = "2", Name = "Mission_Delivery", Expires = 60 } }
        });
        Assert.Equal("Leesti", Assert.Single(integration.CurrentMissions.Active).DestinationSystem);

        integration.HandleMissionCompletedEvent(new MissionCompletedEvent { Timestamp = time, MissionId = "2" });
        Assert.Empty(integration.CurrentMissions.Active);
    }

    [Theory]
    [InlineData("Earthlike body", "Terraformed", 1.0, false, 283629)]
    [InlineData("Earthlike body", "Terraformed", 1.0, true, 737434)]
    [InlineData("Icy body", "", 0.01, false, 500)]
    public void Planet_values_follow_the_community_formula(string planetClass, string terraformState, double mass, bool firstDiscovery, long expected)
    {
        long value = ExplorationValues.EstimatePlanet(planetClass, terraformState, mass, firstDiscovery, false, false, false);
        Assert.InRange(value, expected - 2, expected + 2);
    }

    [Fact]
    public void Mapping_multiplies_a_planets_value()
    {
        long scanned = ExplorationValues.EstimatePlanet("Water world", "Terraformable", 0.5, false, false, false, false);
        long mapped = ExplorationValues.EstimatePlanet("Water world", "Terraformable", 0.5, false, true, false, false);
        long efficient = ExplorationValues.EstimatePlanet("Water world", "Terraformable", 0.5, false, true, false, true);

        Assert.True(mapped > scanned * 3);
        Assert.True(efficient > mapped);
    }

    [Fact]
    public void Star_values_depend_on_type_and_mass()
    {
        Assert.Equal(1218, ExplorationValues.EstimateStar("G", 1.0, false));
        Assert.True(ExplorationValues.EstimateStar("N", 1.0, false) > ExplorationValues.EstimateStar("DA", 1.0, false));
        Assert.Equal((long)Math.Round(1218.1132 * 2.6), ExplorationValues.EstimateStar("G", 1.0, true));
    }

    [Fact]
    public void System_exploration_snapshot_clones_deeply()
    {
        var source = new SystemExplorationData
        {
            SystemName = "Sol",
            Bodies = new[] { new ExploredBodyData { BodyName = "Earth", Signals = new[] { new BodySignalData { Type = "Biological", Count = 3 } } } }
        };

        var clone = (SystemExplorationData)GameDataSnapshot.Clone(GameEventType.SystemExploration, source);

        Assert.NotSame(source, clone);
        Assert.Equal("Earth", clone.Bodies[0].BodyName);
        Assert.Equal(3, clone.Bodies[0].Signals[0].Count);
    }
}
