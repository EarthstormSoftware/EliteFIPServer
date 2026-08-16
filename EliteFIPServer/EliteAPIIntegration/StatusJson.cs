using Newtonsoft.Json;

namespace EliteFIPServer {

    // Bit positions of the Status.json "Flags" field, in EliteAPI's ShipFlags order.
    internal enum ShipFlag {
        Docked = 0, Landed = 1, Gear = 2, Shields = 3, Supercruise = 4, FlightAssistOff = 5,
        Hardpoints = 6, Winging = 7, Lights = 8, CargoScoop = 9, SilentRunning = 10, Scooping = 11,
        SrvHandbrake = 12, SrvTurret = 13, SrvNearShip = 14, SrvDriveAssist = 15, MassLocked = 16,
        FsdCharging = 17, FsdCooldown = 18, LowFuel = 19, Overheating = 20, HasLatLong = 21,
        InDanger = 22, InInterdiction = 23, InMothership = 24, InFighter = 25, InSrv = 26,
        AnalysisMode = 27, NightVision = 28, AltitudeFromAverageRadius = 29, FsdJump = 30, SrvHighBeam = 31
    }

    // Bit positions of the Status.json "Flags2" field, in EliteAPI's CommanderFlags order.
    internal enum CommanderFlag {
        OnFoot = 0, InTaxi = 1, InMultiCrew = 2, OnFootInStation = 3, OnFootOnPlanet = 4,
        AimDownSight = 5, LowOxygen = 6, LowHealth = 7, Cold = 8, Hot = 9, VeryCold = 10, VeryHot = 11
    }

    // EliteAPI v5 no longer exposes a typed Status event - Status.json/NavRoute.json are
    // only available as raw JSON via EliteDangerousApi.OnJson(). These DTOs replicate the
    // shape of the old typed EliteAPI.Events.Status.Ship.StatusEvent / NavRouteEvent.
    internal class StatusJson {

        private static readonly string[] GuiFocusNames = {
            "NoFocus", "InternalPanel", "ExternalPanel", "CommsPanel", "RolePanel",
            "StationServices", "GalaxyMap", "SystemMap", "Orrery", "FssMode", "SaaMode", "Codex"
        };

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("Flags")]
        public uint Flags { get; set; }

        [JsonProperty("Flags2")]
        public uint Flags2 { get; set; }

        [JsonProperty("Pips")]
        public int[] Pips { get; set; }

        [JsonProperty("FireGroup")]
        public long FireGroup { get; set; }

        [JsonProperty("GuiFocus")]
        public int GuiFocus { get; set; }

        [JsonProperty("Fuel")]
        public StatusFuelJson Fuel { get; set; }

        [JsonProperty("Cargo")]
        public double Cargo { get; set; }

        [JsonProperty("LegalState")]
        public string LegalState { get; set; }

        [JsonProperty("Balance")]
        public long Balance { get; set; }

        [JsonProperty("Destination")]
        public StatusDestinationJson Destination { get; set; } = new StatusDestinationJson();

        [JsonProperty("PlanetRadius")]
        public double PlanetRadius { get; set; }

        [JsonProperty("Oxygen")]
        public double Oxygen { get; set; }

        [JsonProperty("Health")]
        public double Health { get; set; }

        [JsonProperty("Temperature")]
        public double Temperature { get; set; }

        [JsonProperty("SelectedWeapon")]
        public string SelectedWeapon { get; set; }

        [JsonProperty("Gravity")]
        public double Gravity { get; set; }

        [JsonProperty("Latitude")]
        public double Latitude { get; set; }

        [JsonProperty("Longitude")]
        public double Longitude { get; set; }

        [JsonProperty("Heading")]
        public double Heading { get; set; }

        [JsonProperty("Altitude")]
        public double Altitude { get; set; }

        [JsonProperty("BodyName")]
        public string BodyName { get; set; }

        public bool Available => Flags != 0 || Flags2 != 0;

        public bool GetShipFlag(ShipFlag flag) => (Flags & (1u << (int)flag)) != 0;

        public bool GetCommanderFlag(CommanderFlag flag) => (Flags2 & (1u << (int)flag)) != 0;

        public string GuiFocusName => (GuiFocus >= 0 && GuiFocus < GuiFocusNames.Length) ? GuiFocusNames[GuiFocus] : GuiFocus.ToString();
    }

    internal class StatusFuelJson {
        [JsonProperty("FuelMain")]
        public double FuelMain { get; set; }

        [JsonProperty("FuelReservoir")]
        public double FuelReservoir { get; set; }
    }

    internal class StatusDestinationJson {
        [JsonProperty("System")]
        public string SystemId { get; set; } = "";

        [JsonProperty("Body")]
        public string BodyId { get; set; } = "";

        [JsonProperty("Name")]
        public string Name { get; set; } = "";
    }

    internal class NavRouteJson {
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("Route")]
        public List<NavRouteStopJson> Route { get; set; } = new List<NavRouteStopJson>();
    }

    internal class NavRouteStopJson {
        [JsonProperty("StarSystem")]
        public string StarSystem { get; set; }

        [JsonProperty("SystemAddress")]
        public string SystemAddress { get; set; }

        [JsonProperty("StarClass")]
        public string StarClass { get; set; }
    }
}
