namespace EliteFIPServer;

// The original game state DTOs, formerly in the EliteFIPProtocol package. Unlike the newer panel models they
// are mutable, because EliteAPIIntegration updates them in place; GameDataSnapshot clones them before queuing.
// Names and properties are unchanged, so the JSON sent to panels is the same.

public class StatusData
{
    public DateTime LastUpdate { get; set; }

    // Flags
    public bool Docked { get; set; }
    public bool Landed { get; set; }
    public bool LandingGearDown { get; set; }
    public bool ShieldsUp { get; set; }
    public bool Supercruise { get; set; }
    public bool FlightAssistOff { get; set; }
    public bool HardpointsDeployed { get; set; }
    public bool InWing { get; set; }
    public bool LightsOn { get; set; }
    public bool CargoScoopDeployed { get; set; }
    public bool SilentRunning { get; set; }
    public bool ScoopingFuel { get; set; }
    public bool SrvHandbrake { get; set; }
    public bool SrvTurret { get; set; }
    public bool SrvUnderShip { get; set; }
    public bool SrvDriveAssist { get; set; }
    public bool FsdMassLocked { get; set; }
    public bool FsdCharging { get; set; }
    public bool FsdCooldown { get; set; }
    public bool LowFuel { get; set; }
    public bool Overheating { get; set; }
    public bool HasLatLong { get; set; }
    public bool InDanger { get; set; }
    public bool BeingInterdicted { get; set; }
    public bool InMainShip { get; set; }
    public bool InFighter { get; set; }
    public bool InSRV { get; set; }
    public bool HudAnalysisMode { get; set; }
    public bool NightVision { get; set; }
    public bool AltitudeFromAverageRadius { get; set; }
    public bool FsdJump { get; set; }
    public bool SrvHighBeam { get; set; }

    // Flags 2
    public bool OnFoot { get; set; }
    public bool InTaxi { get; set; }
    public bool InMulticrew { get; set; }
    public bool OnFootInStation { get; set; }
    public bool OnFootOnPlanet { get; set; }
    public bool AimDownSight { get; set; }
    public bool LowOxygen { get; set; }
    public bool LowHealth { get; set; }
    public bool Cold { get; set; }
    public bool Hot { get; set; }
    public bool VeryCold { get; set; }
    public bool VeryHot { get; set; }
    public bool GlideMode { get; set; }
    public bool OnFootInHanger { get; set; }
    public bool OnFootSocialSpace { get; set; }
    public bool OnFootExterior { get; set; }
    public bool BreathableAtmosphere { get; set; }

    // Status
    public long SystemPips { get; set; }
    public long EnginePips { get; set; }
    public long WeaponPips { get; set; }
    public long FireGroup { get; set; }
    public string GuiFocus { get; set; }
    public double FuelMain { get; set; }
    public double FuelReservoir { get; set; }
    public double Cargo { get; set; }
    public string LegalState { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }
    public double Heading { get; set; }
    public string BodyName { get; set; }
    public double PlanetRadius { get; set; }
    public long Balance { get; set; }
    public string DestinationSystem { get; set; }
    public string DestinationBody { get; set; }
    public string DestinationName { get; set; }
    public double Oxygen { get; set; }
    public double Health { get; set; }
    public double Temperature { get; set; }
    public string SelectedWeapon { get; set; }
    public double Gravity { get; set; }
}

// How much is filled in depends on the scan stage: 0 ship; 1 pilot name and rank; 2 shield and hull health;
// 3 faction, legal status, bounty and subsystem.
public class ShipTargetedData
{
    public DateTime LastUpdate { get; set; }
    public bool TargetLocked { get; set; }
    public string Ship { get; set; }
    public int ScanStage { get; set; }
    public string PilotName { get; set; }
    public string PilotRank { get; set; }
    public string SquadronId { get; set; }
    public double ShieldHealth { get; set; }
    public double HullHealth { get; set; }
    public string Faction { get; set; }
    public string LegalStatus { get; set; }
    public int Bounty { get; set; }
    public string SubSystem { get; set; }
    public double SubSystemHealth { get; set; }
    public string Power { get; set; }
}

public class LocationData
{
    public DateTime LastUpdate { get; set; }
    public string SystemId { get; set; }
    public string SystemName { get; set; }
    public string BodyId { get; set; }
    public string BodyName { get; set; }
    public string MarketId { get; set; }
    public string StationName { get; set; }
    public string StationType { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DistanceFromStarInLightSeconds { get; set; }
    public bool IsDocked { get; set; }
    public bool IsInTaxi { get; set; }
    public bool IsInMultiCrew { get; set; }
    public bool IsInSrv { get; set; }
    public bool IsOnFoot { get; set; }
}

public class NavigationData
{
    public DateTime LastUpdate { get; set; }
    public bool NavRouteActive { get; set; }
    public string LastSystemReached { get; set; }
    public List<NavRouteStop> Stops { get; set; } = new List<NavRouteStop>();

    public class NavRouteStop
    {
        public string SystemId { get; set; }
        public string SystemName { get; set; }
        public string Class { get; set; }
        // Galactic coordinates in light years; empty for routes saved before they were recorded.
        public double[] StarPos { get; set; } = Array.Empty<double>();
        // Light years from the previous stop; zero for the first stop, or when coordinates are missing.
        public double JumpDistance { get; set; }
    }
}

public class JumpData
{
    public DateTime LastUpdate { get; set; }
    public bool JumpComplete { get; set; }
    public string OriginSystemId { get; set; }
    public string OriginSystemName { get; set; }
    public string DestinationSystemId { get; set; }
    public string DestinationSystemName { get; set; }
    public string DestinationSystemClass { get; set; }
    public double JumpDistance { get; set; }
    public double FuelUsed { get; set; }
    public double FuelLevel { get; set; }
    public int BoostUsed { get; set; }
}

public class ReceivedTextData
{
    public DateTime LastUpdate { get; set; }
    public string Channel { get; set; }
    public string Source { get; set; }
    public string Message { get; set; }
}
