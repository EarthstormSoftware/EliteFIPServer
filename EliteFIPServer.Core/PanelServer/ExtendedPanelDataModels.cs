namespace EliteFIPServer;

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
