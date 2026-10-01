namespace EliteFIPServer;

public sealed class StationData
{
    public DateTime LastUpdate { get; init; }
    public string SystemId { get; init; }
    public string SystemName { get; init; }
    public string StationName { get; init; }
    public string StationType { get; init; }
    public string MarketId { get; init; }
    public string Faction { get; init; }
    public string Government { get; init; }
    public string Allegiance { get; init; }
    public string Economy { get; init; }
    public IReadOnlyList<string> Services { get; init; } = Array.Empty<string>();
    public double DistanceFromStarInLightSeconds { get; init; }
    public bool IsDocked { get; init; }
    public bool HasActiveFine { get; init; }
    public bool IsWanted { get; init; }
    public bool HasBreachedCockpit { get; init; }
    public string StationState { get; init; }
    public long SmallLandingPads { get; init; }
    public long MediumLandingPads { get; init; }
    public long LargeLandingPads { get; init; }
}

public sealed class ExplorationData
{
    public DateTime LastUpdate { get; init; }
    public string SystemId { get; init; }
    public string SystemName { get; init; }
    public string BodyId { get; init; }
    public string BodyName { get; init; }
    public string BodyType { get; init; }
    public string StarType { get; init; }
    public string PlanetClass { get; init; }
    public string Atmosphere { get; init; }
    public double SurfaceGravity { get; init; }
    public double SurfaceTemperature { get; init; }
    public double DistanceFromArrivalLs { get; init; }
    public bool IsLandable { get; init; }
    public bool WasDiscovered { get; init; }
    public bool WasMapped { get; init; }
    public string TerraformState { get; init; }
    public string AtmosphereType { get; init; }
    public string Volcanism { get; init; }
    public double SurfacePressure { get; init; }
    public double Radius { get; init; }
    public double StellarMass { get; init; }
    public string Luminosity { get; init; }
    public string ReserveLevel { get; init; }
    public IReadOnlyList<BodyMaterialData> Materials { get; init; } = Array.Empty<BodyMaterialData>();
    public IReadOnlyList<BodyRingData> Rings { get; init; } = Array.Empty<BodyRingData>();
}

public sealed class BodyMaterialData
{
    public string Name { get; init; }
    public double Percent { get; init; }
}

public sealed class BodyRingData
{
    public string Name { get; init; }
    public string RingClass { get; init; }
}

public sealed class LoadoutData
{
    public DateTime LastUpdate { get; init; }
    public string Ship { get; init; }
    public string ShipId { get; init; }
    public string ShipName { get; init; }
    public string ShipIdent { get; init; }
    public double HullHealth { get; init; }
    public long CargoCapacity { get; init; }
    public double MaxJumpRange { get; init; }
    public double MainFuelCapacity { get; init; }
    public double ReserveFuelCapacity { get; init; }
    public long Rebuy { get; init; }
    public IReadOnlyList<LoadoutModuleData> Modules { get; init; } = Array.Empty<LoadoutModuleData>();
}

public sealed class LoadoutModuleData
{
    public string Slot { get; init; }
    public string Item { get; init; }
    public bool IsOn { get; init; }
    public long Priority { get; init; }
    public long Value { get; init; }
    public double Health { get; init; }
    public long AmmoInClip { get; init; }
    public long AmmoInHopper { get; init; }
    public ModuleEngineeringData Engineering { get; init; }
}

public sealed class ModuleEngineeringData
{
    public string Engineer { get; init; }
    public string BlueprintName { get; init; }
    public string ExperimentalEffect { get; init; }
    public long Level { get; init; }
    public double Quality { get; init; }
    public IReadOnlyList<ModuleModifierData> Modifiers { get; init; } = Array.Empty<ModuleModifierData>();
}

public sealed class ModuleModifierData
{
    public string Label { get; init; }
    public double Value { get; init; }
    public double OriginalValue { get; init; }
    public bool LessIsGood { get; init; }
}

public sealed class RouteTargetData
{
    public DateTime LastUpdate { get; init; }
    public string SystemId { get; init; }
    public string SystemName { get; init; }
    public string StarClass { get; init; }
    public long RemainingJumps { get; init; }
}

public sealed class MissionData
{
    public DateTime LastUpdate { get; init; }
    public string MissionId { get; init; }
    public string Name { get; init; }
    public string LocalisedName { get; init; }
    public string Faction { get; init; }
    public string Target { get; init; }
    public string TargetType { get; init; }
    public string DestinationSystem { get; init; }
    public string DestinationStation { get; init; }
    public DateTime Expiry { get; init; }
    public long Reward { get; init; }
    public int Count { get; init; }
    public string Commodity { get; init; }
    public bool IsWing { get; init; }
}

public sealed class MissionCollectionData
{
    public DateTime LastUpdate { get; init; }
    public IReadOnlyList<MissionSummaryData> Active { get; init; } = Array.Empty<MissionSummaryData>();
    public IReadOnlyList<MissionSummaryData> Failed { get; init; } = Array.Empty<MissionSummaryData>();
    public IReadOnlyList<MissionSummaryData> Complete { get; init; } = Array.Empty<MissionSummaryData>();
}

public sealed class MissionSummaryData
{
    public string MissionId { get; init; }
    public string Name { get; init; }
    public bool IsPassengerMission { get; init; }
    public long ExpiresInSeconds { get; init; }
    // Filled from MissionAccepted when it was seen; missions only listed by the Missions event at login
    // have just the fields above, with Expiry worked out from ExpiresInSeconds.
    public DateTime Expiry { get; init; }
    public string Faction { get; init; }
    public string DestinationSystem { get; init; }
    public string DestinationStation { get; init; }
    public long Reward { get; init; }
    public int Count { get; init; }
    public string Commodity { get; init; }
    public string Target { get; init; }
    public bool IsWing { get; init; }
}

public sealed class DockingData
{
    public DateTime LastUpdate { get; init; }
    public string Status { get; init; }
    public string StationName { get; init; }
    public string StationType { get; init; }
    public string MarketId { get; init; }
    public long LandingPad { get; init; }
    public string Reason { get; init; }
}
