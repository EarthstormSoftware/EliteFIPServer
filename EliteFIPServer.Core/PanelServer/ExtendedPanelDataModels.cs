namespace EliteFIPServer;

// Everything scanned in the current system; reset when the commander arrives in another system.
public sealed class SystemExplorationData
{
    public DateTime LastUpdate { get; init; }
    public string SystemId { get; init; }
    public string SystemName { get; init; }
    // From the FSS discovery scan (honk); zero until one has been done in this system.
    public long BodyCount { get; init; }
    public long NonBodyCount { get; init; }
    public double DiscoveryProgress { get; init; }
    public bool AllBodiesFound { get; init; }
    // Estimates of what the scanned bodies would sell for, as scanned and mapped so far.
    public long EstimatedValue { get; init; }
    public IReadOnlyList<ExploredBodyData> Bodies { get; init; } = Array.Empty<ExploredBodyData>();
}

public sealed class ExploredBodyData
{
    public string BodyId { get; init; }
    public string BodyName { get; init; }
    public string BodyType { get; init; }
    public string StarType { get; init; }
    public string PlanetClass { get; init; }
    public string TerraformState { get; init; }
    public double DistanceFromArrivalLs { get; init; }
    public bool IsLandable { get; init; }
    public bool WasDiscovered { get; init; }
    public bool WasMapped { get; init; }
    public bool IsMapped { get; init; }
    public bool MappedEfficiently { get; init; }
    public long EstimatedValue { get; init; }
    // What the body would be worth once mapped; zero for stars, which can't be mapped.
    public long EstimatedMappedValue { get; init; }
    public IReadOnlyList<BodySignalData> Signals { get; init; } = Array.Empty<BodySignalData>();
}

public sealed class BodySignalData
{
    public string Type { get; init; }
    public long Count { get; init; }
}

// Organic scans: the species being sampled now, and analysed species not yet sold (lost if the commander dies).
public sealed class ExobiologyData
{
    public DateTime LastUpdate { get; init; }
    // Null when no species is part-way through sampling.
    public BioScanData Current { get; init; }
    public IReadOnlyList<BioScanData> Unsold { get; init; } = Array.Empty<BioScanData>();
    public long UnsoldEstimatedValue { get; init; }
    // What Vista Genomics paid at the last sale, bonuses included.
    public long LastSaleValue { get; init; }
}

public sealed class BioScanData
{
    public string SystemId { get; init; }
    public string BodyId { get; init; }
    public string BodyName { get; init; }
    public string Genus { get; init; }
    public string Species { get; init; }
    public string Variant { get; init; }
    // 1 to 3; analysed species have 3.
    public int SamplesTaken { get; init; }
    public long SampleDistance { get; init; }
    public long EstimatedValue { get; init; }
    // Where each sample was taken, when known; samples replayed at startup have no position.
    public IReadOnlyList<BioSamplePositionData> Samples { get; init; } = Array.Empty<BioSamplePositionData>();
}

public sealed class BioSamplePositionData
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public double PlanetRadius { get; init; }
}

public sealed class MissionLifecycleData
{
    public DateTime LastUpdate { get; init; }
    public string Operation { get; init; }
    public string MissionId { get; init; }
    public string Name { get; init; }
    public string LocalisedName { get; init; }
    public long Reward { get; init; }
    public long Fine { get; init; }
}

public sealed class CargoData
{
    public DateTime LastUpdate { get; init; }
    public double Capacity { get; init; }
    public double Total { get; init; }
    public IReadOnlyList<CargoItemData> Items { get; init; } = Array.Empty<CargoItemData>();
}

public sealed class CargoItemData
{
    public string Name { get; init; }
    public long Count { get; init; }
    public bool IsStolen { get; init; }
    public string MissionId { get; init; }
}

public sealed class MaterialsData
{
    public DateTime LastUpdate { get; init; }
    public IReadOnlyList<MaterialItemData> Raw { get; init; } = Array.Empty<MaterialItemData>();
    public IReadOnlyList<MaterialItemData> Manufactured { get; init; } = Array.Empty<MaterialItemData>();
    public IReadOnlyList<MaterialItemData> Encoded { get; init; } = Array.Empty<MaterialItemData>();
}

public sealed class MaterialItemData
{
    public string Name { get; init; }
    public long Count { get; init; }
    public string Category { get; init; }
    // 1 to 5, and the storage limit for that grade; 0 when the material isn't in MaterialGrades.
    public int Grade { get; init; }
    public long Maximum { get; init; }
}

// Ranks with progress to the next, superpower reputation, Powerplay, and engineer access.
public sealed class CommanderData
{
    public DateTime LastUpdate { get; init; }
    // Combat, Trade, Explore, Soldier, Exobiologist, CQC, Federation, Empire; Rank is the game's number.
    public IReadOnlyList<CommanderRankData> Ranks { get; init; } = Array.Empty<CommanderRankData>();
    // -100 to 100.
    public IReadOnlyList<SuperpowerReputationData> Reputation { get; init; } = Array.Empty<SuperpowerReputationData>();
    public string Power { get; init; }
    public long PowerplayRank { get; init; }
    public long Merits { get; init; }
    public long TimePledgedSeconds { get; init; }
    public IReadOnlyList<EngineerData> Engineers { get; init; } = Array.Empty<EngineerData>();
}

public sealed class CommanderRankData
{
    public string Name { get; init; }
    public long Rank { get; init; }
    // Percent towards the next rank.
    public long Progress { get; init; }
}

public sealed class SuperpowerReputationData
{
    public string Superpower { get; init; }
    public double Reputation { get; init; }
}

public sealed class EngineerData
{
    public string Name { get; init; }
    // Known, Invited, Acquainted or Unlocked.
    public string Progress { get; init; }
    public long Rank { get; init; }
    public long RankProgress { get; init; }
}

// Bounty vouchers and combat bonds earned but not yet handed in. They are kept until redeemed and lost on death.
public sealed class CombatEarningsData
{
    public DateTime LastUpdate { get; init; }
    public long UnredeemedBounties { get; init; }
    public long BountyCount { get; init; }
    public long UnredeemedBonds { get; init; }
    public long BondCount { get; init; }
    public long LastRedeemed { get; init; }
}

// This game session's mining: the last prospector limpet result and tons refined.
public sealed class MiningData
{
    public DateTime LastUpdate { get; init; }
    public ProspectedAsteroidData LastProspect { get; init; }
    // When the first ton was refined this session, for a rate.
    public DateTime FirstRefined { get; init; }
    public long TotalRefined { get; init; }
    public IReadOnlyList<RefinedCommodityData> Refined { get; init; } = Array.Empty<RefinedCommodityData>();
}

public sealed class ProspectedAsteroidData
{
    public DateTime Timestamp { get; init; }
    // High, Medium or Low.
    public string Content { get; init; }
    public string Motherlode { get; init; }
    public double Remaining { get; init; }
    public IReadOnlyList<ProspectedMaterialData> Materials { get; init; } = Array.Empty<ProspectedMaterialData>();
}

public sealed class ProspectedMaterialData
{
    public string Name { get; init; }
    public double Percent { get; init; }
}

public sealed class RefinedCommodityData
{
    public string Name { get; init; }
    public long Count { get; init; }
}

// This game session's trading, and what the last market visited pays for the cargo held now.
public sealed class TradeData
{
    public DateTime LastUpdate { get; init; }
    public long SessionSales { get; init; }
    // Sales less what was paid for the goods; mined and salvaged goods count in full.
    public long SessionProfit { get; init; }
    public long SessionPurchases { get; init; }
    public string LastSale { get; init; }
    public string MarketStation { get; init; }
    public string MarketSystem { get; init; }
    public DateTime MarketUpdated { get; init; }
    public IReadOnlyList<CargoPriceData> CargoPrices { get; init; } = Array.Empty<CargoPriceData>();
}

public sealed class CargoPriceData
{
    public string Name { get; init; }
    public long Count { get; init; }
    public long SellPrice { get; init; }
    public long MeanPrice { get; init; }
    public long Demand { get; init; }
}

// The commander's own fleet carrier, from the carrier management screen and jump requests.
public sealed class CarrierData
{
    public DateTime LastUpdate { get; init; }
    public string Name { get; init; }
    public string Callsign { get; init; }
    public long FuelLevel { get; init; }
    public long Balance { get; init; }
    public long ReserveBalance { get; init; }
    public long AvailableBalance { get; init; }
    public long FreeSpace { get; init; }
    public long TotalCapacity { get; init; }
    public string DockingAccess { get; init; }
    public bool IsPendingDecommission { get; init; }
    // Set while a jump is scheduled; cleared when it is cancelled.
    public string JumpDestinationSystem { get; init; }
    public string JumpDestinationBody { get; init; }
    public DateTime? JumpDeparture { get; init; }
}

// Odyssey on-foot equipment: the suit loadout and micro-resources in the ship locker and backpack.
public sealed class OnFootData
{
    public DateTime LastUpdate { get; init; }
    public string SuitName { get; init; }
    public string LoadoutName { get; init; }
    public IReadOnlyList<string> Weapons { get; init; } = Array.Empty<string>();
    public IReadOnlyList<LockerItemData> ShipLocker { get; init; } = Array.Empty<LockerItemData>();
    public IReadOnlyList<LockerItemData> Backpack { get; init; } = Array.Empty<LockerItemData>();
}

public sealed class LockerItemData
{
    public string Name { get; init; }
    // Items, Components, Consumables or Data.
    public string Category { get; init; }
    public long Count { get; init; }
}

public sealed class CombatData
{
    public DateTime LastUpdate { get; init; }
    public string Operation { get; init; }
    public double HullHealth { get; init; }
    public bool IsPlayerPilot { get; init; }
    public bool IsFighter { get; init; }
    public bool ShieldsUp { get; init; }
    public string Target { get; init; }
    public long Reward { get; init; }
    public string Faction { get; init; }
    public bool IsInterdiction { get; init; }
    public bool IsSuccessful { get; init; }
}

public sealed class SystemData
{
    public DateTime LastUpdate { get; init; }
    public string SystemId { get; init; }
    public string SystemName { get; init; }
    public string SystemAllegiance { get; init; }
    public string Economy { get; init; }
    public string SecondaryEconomy { get; init; }
    public string Government { get; init; }
    public string Security { get; init; }
    public long Population { get; init; }
    public IReadOnlyList<double> StarPosition { get; init; } = Array.Empty<double>();
    public IReadOnlyList<string> Powers { get; init; } = Array.Empty<string>();
    public string PowerplayState { get; init; }
    public string ControllingFaction { get; init; }
    public IReadOnlyList<SystemFactionData> Factions { get; init; } = Array.Empty<SystemFactionData>();
}

public sealed class SystemFactionData
{
    public string Name { get; init; }
    public string State { get; init; }
    public string Government { get; init; }
    public double Influence { get; init; }
    public string Allegiance { get; init; }
    public string Happiness { get; init; }
    public double Reputation { get; init; }
    public IReadOnlyList<string> ActiveStates { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RecoveringStates { get; init; } = Array.Empty<string>();
}
