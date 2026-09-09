using Xunit;

namespace EliteFIPServer.Tests;

public class ExpandedPanelDataTests
{
    [Fact]
    public void Mission_lifecycle_preserves_operation_and_identity()
    {
        var data = new MissionLifecycleData
        {
            Operation = "Completed",
            MissionId = "123",
            Reward = 50000
        };

        Assert.Equal("Completed", data.Operation);
        Assert.Equal("123", data.MissionId);
        Assert.Equal(50000, data.Reward);
    }

    [Fact]
    public void System_data_preserves_powerplay_and_faction_state()
    {
        var data = new SystemData
        {
            SystemName = "Sol",
            PowerplayState = "Controlled",
            Powers = new[] { "Aisling Duval" },
            Factions = new[] { new SystemFactionData { Name = "Local Faction", Influence = 0.42 } }
        };

        Assert.Equal("Controlled", data.PowerplayState);
        Assert.Single(data.Powers);
        Assert.Equal(0.42, data.Factions[0].Influence);
    }

    [Fact]
    public void Combat_data_identifies_damage_event()
    {
        var data = new CombatData
        {
            Operation = "HullDamage",
            HullHealth = 0.75,
            IsPlayerPilot = true
        };

        Assert.Equal("HullDamage", data.Operation);
        Assert.Equal(0.75, data.HullHealth);
        Assert.True(data.IsPlayerPilot);
    }

    [Fact]
    public void Loadout_module_preserves_operational_and_engineering_details()
    {
        var module = new LoadoutModuleData
        {
            Slot = "MainEngines",
            Priority = 2,
            Value = 1250000,
            AmmoInClip = 1,
            AmmoInHopper = 4,
            Engineering = new ModuleEngineeringData
            {
                BlueprintName = "FSD_LongRange",
                Level = 5,
                ExperimentalEffect = "Mass Manager"
            }
        };

        Assert.Equal(2, module.Priority);
        Assert.Equal(1250000, module.Value);
        Assert.Equal(5, module.Engineering.Level);
        Assert.Equal("Mass Manager", module.Engineering.ExperimentalEffect);
    }

    [Fact]
    public void Route_target_preserves_live_jump_guidance()
    {
        var data = new RouteTargetData
        {
            SystemName = "Colonia",
            StarClass = "K",
            RemainingJumps = 17
        };

        Assert.Equal("Colonia", data.SystemName);
        Assert.Equal("K", data.StarClass);
        Assert.Equal(17, data.RemainingJumps);
    }

    [Fact]
    public void Mission_collection_preserves_active_mission_state()
    {
        var data = new MissionCollectionData
        {
            Active = new[] { new MissionSummaryData { MissionId = "42", Name = "Mission_Delivery", ExpiresInSeconds = 3600 } }
        };

        Assert.Single(data.Active);
        Assert.Equal(3600, data.Active[0].ExpiresInSeconds);
    }

    [Fact]
    public void Docking_data_preserves_landing_guidance()
    {
        var data = new DockingData { Status = "Granted", StationName = "Jameson Memorial", LandingPad = 42 };

        Assert.Equal("Granted", data.Status);
        Assert.Equal(42, data.LandingPad);
    }

    [Theory]
    [InlineData("$economy_Industrial;", "Industrial")]
    [InlineData("$economy_Agri;", "Agriculture")]
    [InlineData("$SYSTEM_SECURITY_high;", "High Security")]
    [InlineData("Mission_Delivery", "Delivery")]
    [InlineData("crewlounge", "Crew Lounge")]
    [InlineData("vistagenomics", "Vista Genomics")]
    [InlineData("Weapon_HighCapacity", "High Capacity")]
    [InlineData("hpt_multicannon_gimbal_huge", "Huge Multi-Cannon Gimballed")]
    [InlineData("High Security", "High Security")]
    public void Journal_display_text_is_user_friendly(string value, string expected)
    {
        Assert.Equal(expected, JournalDisplayText.Format(value));
    }
}
