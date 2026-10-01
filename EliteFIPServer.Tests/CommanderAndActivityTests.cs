using EliteAPI.Events.Game;
using EliteAPI.Journals;
using EliteAPI.Json;
using Newtonsoft.Json;
using Xunit;

namespace EliteFIPServer.Tests;

public class CommanderAndActivityTests
{
    private static EliteAPIIntegration StartedIntegration()
    {
        var server = new CoreServer(Array.Empty<string>());
        server.EliteAPIIntegration.CurrentState.Set(RunState.Started);
        return server.EliteAPIIntegration;
    }

    private static T JournalEvent<T>(string json)
    {
        JournalUtils.PrepareLocalisations(json);
        return JsonConvert.DeserializeObject<T>(json, JsonUtils.SerializerSettings);
    }

    private static readonly DateTime Time = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Ranks_progress_promotions_and_reputation_build_the_commander()
    {
        var integration = StartedIntegration();
        integration.HandleRankEvent(new RankEvent { Timestamp = Time, Combat = 3, Trade = 8, Explore = 9, Exobiologist = 2, Federation = 5 });
        integration.HandleProgressEvent(new ProgressEvent { Timestamp = Time, Combat = 40, Trade = 75 });
        integration.HandlePromotionJson(("Promotion", """{ "timestamp":"2026-10-01T13:00:00Z", "event":"Promotion", "Combat":4 }"""));
        integration.HandleReputationEvent(new ReputationEvent { Timestamp = Time, Federation = 92.5, Empire = -10 });
        integration.HandlePowerplayEvent(new PowerplayEvent { Timestamp = Time, Power = "Aisling Duval", Rank = 12, Merits = 3400 });

        var commander = integration.CurrentCommander;
        var combat = commander.Ranks.Single(rank => rank.Name == "Combat");
        Assert.Equal(4, combat.Rank);
        Assert.Equal(0, combat.Progress);
        Assert.Equal(75, commander.Ranks.Single(rank => rank.Name == "Trade").Progress);
        Assert.Equal(new[] { "Combat", "Trade", "Explore", "Soldier", "Exobiologist", "CQC", "Federation", "Empire" }, commander.Ranks.Select(rank => rank.Name));
        Assert.Equal(92.5, commander.Reputation.Single(item => item.Superpower == "Federation").Reputation);
        Assert.Equal("Aisling Duval", commander.Power);
        Assert.Equal(3400, commander.Merits);
    }

    [Fact]
    public void Engineer_progress_accepts_the_login_list_and_single_updates()
    {
        var integration = StartedIntegration();
        integration.HandleEngineerProgressJson(("EngineerProgress", """
            { "timestamp":"2026-10-01T12:00:00Z", "event":"EngineerProgress", "Engineers":[
              { "Engineer":"Felicity Farseer", "EngineerID":300100, "Progress":"Unlocked", "RankProgress":0, "Rank":5 },
              { "Engineer":"Elvira Martuuk", "EngineerID":300160, "Progress":"Known" } ] }
            """));
        integration.HandleEngineerProgressJson(("EngineerProgress", """
            { "timestamp":"2026-10-01T12:05:00Z", "event":"EngineerProgress", "Engineer":"Elvira Martuuk", "EngineerID":300160, "Progress":"Invited" }
            """));

        var engineers = integration.CurrentCommander.Engineers;
        Assert.Equal(2, engineers.Count);
        Assert.Equal("Invited", engineers.Single(engineer => engineer.Name == "Elvira Martuuk").Progress);
        Assert.Equal(5, engineers.Single(engineer => engineer.Name == "Felicity Farseer").Rank);
    }

    [Fact]
    public void Combat_earnings_add_up_until_redeemed_or_lost()
    {
        var integration = StartedIntegration();
        integration.HandleBountyEvent(JournalEvent<BountyEvent>("""
            { "timestamp":"2026-10-01T12:00:00Z", "event":"Bounty", "Target":"python", "TotalReward":100000, "VictimFaction":"Pirates" }
            """));
        integration.HandleBountyEvent(JournalEvent<BountyEvent>("""
            { "timestamp":"2026-10-01T12:01:00Z", "event":"Bounty", "Target":"skimmer", "Reward":20000, "VictimFaction":"Pirates" }
            """));
        integration.HandleFactionKillBondEvent(new FactionKillBondEvent { Timestamp = Time, Reward = 50000 });

        Assert.Equal(120000, integration.CurrentCombatEarnings.UnredeemedBounties);
        Assert.Equal(2, integration.CurrentCombatEarnings.BountyCount);
        Assert.Equal(50000, integration.CurrentCombatEarnings.UnredeemedBonds);

        // A broker takes 25%, so 90000 handed over settles all 120000 of vouchers.
        integration.HandleRedeemVoucherEvent(new RedeemVoucherEvent { Timestamp = Time, Type = "bounty", Amount = 90000, BrokerPercentage = 25 });
        Assert.Equal(0, integration.CurrentCombatEarnings.UnredeemedBounties);
        Assert.Equal(0, integration.CurrentCombatEarnings.BountyCount);

        integration.HandleDiedEvent(new DiedEvent { Timestamp = Time });
        Assert.Equal(0, integration.CurrentCombatEarnings.UnredeemedBonds);
    }

    [Fact]
    public void Mining_keeps_the_last_prospect_and_tons_refined_per_session()
    {
        var integration = StartedIntegration();
        integration.HandleProspectedAsteroidEvent(JournalEvent<ProspectedAsteroidEvent>("""
            { "timestamp":"2026-10-01T12:00:00Z", "event":"ProspectedAsteroid",
              "Materials":[ { "Name":"platinum", "Proportion":31.5 }, { "Name":"painite", "Name_Localised":"Painite", "Proportion":8.25 } ],
              "MotherlodeMaterial":"$LowTemperatureDiamond_name;", "MotherlodeMaterial_Localised":"Low Temperature Diamonds",
              "Content":"$AsteroidMaterialContent_High;", "Content_Localised":"Material Content: High", "Remaining":100.0 }
            """));
        foreach (var minute in new[] { 1, 2 })
        {
            integration.HandleMiningRefinedEvent(JournalEvent<MiningRefinedEvent>($$"""
                { "timestamp":"2026-10-01T12:0{{minute}}:00Z", "event":"MiningRefined", "Type":"$platinum_name;", "Type_Localised":"Platinum" }
                """));
        }

        var mining = integration.CurrentMining;
        Assert.Equal("High", mining.LastProspect.Content);
        Assert.Equal("Low Temperature Diamonds", mining.LastProspect.Motherlode);
        Assert.Equal(31.5, mining.LastProspect.Materials[0].Percent);
        Assert.Equal(2, mining.TotalRefined);
        Assert.Equal("Platinum", Assert.Single(mining.Refined).Name);
        Assert.Equal(new DateTime(2026, 10, 1, 12, 1, 0, DateTimeKind.Utc), mining.FirstRefined.ToUniversalTime());

        integration.HandleLoadGameEvent(new LoadGameEvent { Timestamp = Time.AddHours(2) });
        Assert.Equal(0, integration.CurrentMining.TotalRefined);
        Assert.Null(integration.CurrentMining.LastProspect);
    }

    [Fact]
    public void Trade_matches_market_prices_to_cargo_and_tracks_session_profit()
    {
        var integration = StartedIntegration();
        integration.HandleMarketJson(("Market", """
            { "timestamp":"2026-10-01T12:00:00Z", "event":"Market", "MarketID":1, "StationName":"Jameson Memorial", "StarSystem":"Shinrarta Dezhra",
              "Items":[ { "Name":"$gold_name;", "Name_Localised":"Gold", "SellPrice":48000, "MeanPrice":47000, "Demand":1200 },
                        { "Name":"$silver_name;", "Name_Localised":"Silver", "SellPrice":5000, "MeanPrice":4800, "Demand":0 } ] }
            """));
        integration.HandleCargoJson(("Cargo", """
            { "timestamp":"2026-10-01T12:01:00Z", "event":"Cargo", "Vessel":"Ship", "Count":20,
              "Inventory":[ { "Name":"gold", "Name_Localised":"Gold", "Count":20, "Stolen":0 } ] }
            """));
        integration.HandleMarketSellEvent(JournalEvent<MarketSellEvent>("""
            { "timestamp":"2026-10-01T12:02:00Z", "event":"MarketSell", "MarketID":1, "Type":"$gold_name;", "Type_Localised":"Gold",
              "Count":10, "SellPrice":48000, "TotalSale":480000, "AvgPricePaid":45000 }
            """));

        var trade = integration.CurrentTrade;
        var gold = Assert.Single(trade.CargoPrices);
        Assert.Equal("Gold", gold.Name);
        Assert.Equal(48000, gold.SellPrice);
        Assert.Equal("Jameson Memorial", trade.MarketStation);
        Assert.Equal(480000, trade.SessionSales);
        Assert.Equal(30000, trade.SessionProfit);
    }

    [Fact]
    public void Carrier_keeps_its_stats_and_scheduled_jump_until_cancelled()
    {
        var integration = StartedIntegration();
        integration.HandleCarrierStatsEvent(JournalEvent<CarrierStatsEvent>("""
            { "timestamp":"2026-10-01T12:00:00Z", "event":"CarrierStats", "CarrierID":1, "Callsign":"X9Z-B0B", "Name":"NORMANDY",
              "DockingAccess":"all", "FuelLevel":800,
              "SpaceUsage":{ "TotalCapacity":25000, "FreeSpace":12000 },
              "Finance":{ "CarrierBalance":2000000000, "ReserveBalance":100000000, "AvailableBalance":1900000000 } }
            """));
        integration.HandleCarrierJumpRequestEvent(new CarrierJumpRequestEvent
        {
            Timestamp = Time, SystemName = "Colonia", Body = "Colonia 1", DepartureTime = "2026-10-01T12:15:10Z"
        });

        var carrier = integration.CurrentCarrier;
        Assert.Equal("NORMANDY", carrier.Name);
        Assert.Equal(800, carrier.FuelLevel);
        Assert.Equal(2000000000, carrier.Balance);
        Assert.Equal("Colonia", carrier.JumpDestinationSystem);
        Assert.Equal(new DateTime(2026, 10, 1, 12, 15, 10, DateTimeKind.Utc), carrier.JumpDeparture);

        integration.HandleCarrierJumpCancelledEvent(new CarrierJumpCancelledEvent { Timestamp = Time });
        Assert.Null(integration.CurrentCarrier.JumpDestinationSystem);
        Assert.Equal("NORMANDY", integration.CurrentCarrier.Name);
    }

    [Fact]
    public void On_foot_reads_the_suit_loadout_and_locker_snapshots()
    {
        var integration = StartedIntegration();
        integration.HandleSuitLoadoutEvent(JournalEvent<SuitLoadoutEvent>("""
            { "timestamp":"2026-10-01T12:00:00Z", "event":"SuitLoadout", "SuitID":1, "SuitName":"tacticalsuit_class3", "SuitName_Localised":"Dominator Suit",
              "LoadoutID":4, "LoadoutName":"Assault",
              "Modules":[ { "SlotName":"PrimaryWeapon1", "SuitModuleID":2, "ModuleName":"wpn_m_assaultrifle_kinetic_fauto", "ModuleName_Localised":"Karma AR-50" } ] }
            """));
        integration.HandleShipLockerJson(("ShipLocker", """
            { "timestamp":"2026-10-01T12:01:00Z", "event":"ShipLocker",
              "Items":[ { "Name":"weaponschematic", "Name_Localised":"Weapon Schematic", "OwnerID":0, "Count":2 } ],
              "Components":[ { "Name":"graphene", "Name_Localised":"Graphene", "OwnerID":0, "Count":12 } ],
              "Consumables":[], "Data":[] }
            """));
        // The journal's ShipLocker line without contents leaves the snapshot alone.
        integration.HandleShipLockerJson(("ShipLocker", """{ "timestamp":"2026-10-01T12:02:00Z", "event":"ShipLocker" }"""));

        var onFoot = integration.CurrentOnFoot;
        Assert.Equal("Dominator Suit", onFoot.SuitName);
        Assert.Equal("Assault", onFoot.LoadoutName);
        Assert.Equal("Karma AR-50", Assert.Single(onFoot.Weapons));
        Assert.Equal(2, onFoot.ShipLocker.Count);
        Assert.Equal(12, onFoot.ShipLocker.Single(item => item.Name == "Graphene").Count);
    }

    [Theory]
    [InlineData("iron", 1, 300)]
    [InlineData("$chemicaldistillery_name;", 3, 200)]
    [InlineData("imperialshielding", 5, 100)]
    [InlineData("guardian_sentinel_weaponparts", 0, 0)]
    public void Material_grades_give_storage_limits(string symbol, int grade, long maximum)
    {
        Assert.Equal(grade, MaterialGrades.Grade(symbol));
        Assert.Equal(maximum, MaterialGrades.Maximum(MaterialGrades.Grade(symbol)));
    }

    [Fact]
    public void Every_new_event_family_has_a_panel_message_and_a_snapshot_clone()
    {
        var families = new (GameEventType Type, object Data)[]
        {
            (GameEventType.Commander, new CommanderData { Power = "x" }),
            (GameEventType.CombatEarnings, new CombatEarningsData { BountyCount = 1 }),
            (GameEventType.Mining, new MiningData { TotalRefined = 1 }),
            (GameEventType.Trade, new TradeData { SessionSales = 1 }),
            (GameEventType.Carrier, new CarrierData { Name = "x" }),
            (GameEventType.OnFoot, new OnFootData { SuitName = "x" })
        };
        foreach (var family in families)
        {
            Assert.True(PanelServer.PanelEventNames.ContainsKey(family.Type), family.Type.ToString());
            var clone = GameDataSnapshot.Clone(family.Type, family.Data);
            Assert.NotSame(family.Data, clone);
            Assert.IsType(family.Data.GetType(), clone);
        }
    }
}
