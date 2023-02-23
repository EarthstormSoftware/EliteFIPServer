using EliteAPI;
using EliteAPI.Abstractions;
using EliteAPI.Abstractions.Events;
using EliteFIPProtocol;
using EliteFIPServer.Logging;

namespace EliteFIPServer {
    public class EliteAPIIntegration {

        private CoreServer CoreServer;
        public ComponentState CurrentState { get; private set; }

        public event EventHandler<RunState> onStateChange;

        // Game State Provider
        private IEliteDangerousApi EliteAPI;

        // Current Game State Information
        private StatusData currentStatus = new StatusData();
        private ShipTargetedData currentTarget = new ShipTargetedData();
        private LocationData currentLocation = new LocationData();
        private NavigationData currentNavRoute = new NavigationData();
        private JumpData currentJump = new JumpData();

        public EliteAPIIntegration(CoreServer coreServer) {
            CoreServer = coreServer;
            CurrentState = new ComponentState();

            EliteAPI = EliteDangerousApi.Create();

            // Add events to watch list            
            EliteAPI.Events.On<EliteAPI.Events.Status.Ship.StatusEvent>(HandleStatusEvent);
            EliteAPI.Events.On<EliteAPI.Events.ShipTargetedEvent>(HandleShipTargetedEvent);
            EliteAPI.Events.On<EliteAPI.Events.LocationEvent>(HandleLocationEvent);
            EliteAPI.Events.On<EliteAPI.Events.StartJumpEvent>(HandleStartJumpEvent);
            EliteAPI.Events.On<EliteAPI.Events.FsdJumpEvent>(HandleFsdJumpEvent);
            EliteAPI.Events.On<EliteAPI.Events.Status.NavRoute.NavRouteEvent>(HandleNavRouteEvent);
            EliteAPI.Events.On<EliteAPI.Events.Status.NavRoute.NavRouteClearEvent>(HandleNavRouteClearEvent);
            EliteAPI.Events.On<EliteAPI.Events.ApproachBodyEvent>(HandleApproachBodyEvent);
            EliteAPI.Events.On<EliteAPI.Events.LeaveBodyEvent>(HandleLeaveBodyEvent);
            EliteAPI.Events.On<EliteAPI.Events.DockedEvent>(HandleDockedEvent);
            EliteAPI.Events.On<EliteAPI.Events.UndockedEvent>(HandleUndockedEvent);
        }

        public void Start() {
            CurrentState.Set(RunState.Starting);
            // Start tracking game events
            EliteAPI.StartAsync();
            CurrentState.Set(RunState.Started);
        }

        public void Stop() {
            CurrentState.Set(RunState.Stopping);
            // Stop tracking game events
            EliteAPI.StopAsync();
            CurrentState.Set(RunState.Stopped);

        }


        public void FullClientUpdate() {
            if (currentStatus != null) { CoreServer.GameDataEvent(GameEventType.Status, currentStatus); }
            if (currentTarget != null) { CoreServer.GameDataEvent(GameEventType.Target, currentTarget); }
            if (currentLocation != null) { CoreServer.GameDataEvent(GameEventType.Location, currentLocation); }
            if (currentNavRoute != null) { CoreServer.GameDataEvent(GameEventType.Navigation, currentNavRoute); }
            if (currentJump != null) { CoreServer.GameDataEvent(GameEventType.Jump, currentJump); }

        }

        public void HandleStatusEvent(EliteAPI.Events.Status.Ship.StatusEvent currentStatusData, EventContext context) {

            Log.Instance.Info("Handling Status Event");

            currentStatus.LastUpdate = DateTime.Now;

            // Original Status Flags
            if (currentStatusData.Available) {
                currentStatus.Docked = currentStatusData.Docked;
                currentStatus.Landed = currentStatusData.Landed;
                currentStatus.LandingGearDown = currentStatusData.Gear;
                currentStatus.ShieldsUp = currentStatusData.Shields;
                currentStatus.Supercruise = currentStatusData.Supercruise;
                currentStatus.FlightAssistOff = !currentStatusData.FlightAssist;
                currentStatus.HardpointsDeployed = currentStatusData.Hardpoints;
                currentStatus.InWing = currentStatusData.Winging;
                currentStatus.LightsOn = currentStatusData.Lights;
                currentStatus.CargoScoopDeployed = currentStatusData.CargoScoop;
                currentStatus.SilentRunning = currentStatusData.SilentRunning;
                currentStatus.ScoopingFuel = currentStatusData.Scooping;
                currentStatus.SrvHandbrake = currentStatusData.SrvHandbrake;
                currentStatus.SrvTurret = currentStatusData.SrvTurret;
                currentStatus.SrvUnderShip = currentStatusData.SrvNearShip;
                currentStatus.SrvDriveAssist = currentStatusData.SrvDriveAssist;
                currentStatus.FsdMassLocked = currentStatusData.MassLocked;
                currentStatus.FsdCharging = currentStatusData.FsdCharging;
                currentStatus.FsdCooldown = currentStatusData.FsdCooldown;
                currentStatus.LowFuel = currentStatusData.LowFuel;
                currentStatus.Overheating = currentStatusData.Overheating;
                if (currentStatusData.HasLatLong) {
                    currentStatus.HasLatLong = true;
                    currentStatus.Latitude = currentStatusData.Latitude;
                    currentStatus.Longitude = currentStatusData.Longitude;
                } else {
                    currentStatus.HasLatLong = false;
                    currentStatus.Latitude = 0;
                    currentStatus.Longitude = 0;
                }
                currentStatus.InDanger = currentStatusData.InDanger;
                currentStatus.BeingInterdicted = currentStatusData.InInterdiction;
                currentStatus.InMainShip = currentStatusData.InMothership;
                currentStatus.InFighter = currentStatusData.InFighter;
                currentStatus.InSRV = currentStatusData.InSrv;
                currentStatus.HudAnalysisMode = currentStatusData.AnalysisMode;
                currentStatus.NightVision = currentStatusData.NightVision;
                currentStatus.FsdJump = currentStatusData.FsdJump;
                currentStatus.AltitudeFromAverageRadius = currentStatusData.AltitudeFromAverageRadius;
                currentStatus.SrvHighBeam = currentStatusData.SrvHighBeam;

                currentStatus.OnFoot = currentStatusData.OnFoot;
                currentStatus.InTaxi = currentStatusData.InTaxi;
                currentStatus.InMulticrew = currentStatusData.InMultiCrew;
                currentStatus.OnFootInStation = currentStatusData.OnFootInStation;
                currentStatus.OnFootOnPlanet = currentStatusData.OnFootOnPlanet;
                currentStatus.AimDownSight = currentStatusData.AimDownSight;
                currentStatus.LowOxygen = currentStatusData.LowOxygen;
                currentStatus.LowHealth = currentStatusData.LowHealth;
                currentStatus.Cold = currentStatusData.Cold;
                currentStatus.Hot = currentStatusData.Hot;
                currentStatus.VeryCold = currentStatusData.VeryCold;
                currentStatus.VeryHot = currentStatusData.VeryHot;

                currentStatus.SystemPips = currentStatusData.Pips.System;
                currentStatus.EnginePips = currentStatusData.Pips.Engines;
                currentStatus.WeaponPips = currentStatusData.Pips.Weapons;
                currentStatus.FireGroup = currentStatusData.FireGroup;
                currentStatus.GuiFocus = currentStatusData.GuiFocus.ToString();
                currentStatus.FuelMain = currentStatusData.Fuel.FuelMain;
                currentStatus.FuelReservoir = currentStatusData.Fuel.FuelReservoir;
                currentStatus.Cargo = currentStatusData.Cargo;
                currentStatus.LegalState = currentStatusData.LegalState.ToString();
                currentStatus.Altitude = currentStatusData.Altitude;
                currentStatus.Heading = currentStatusData.Heading;
                currentStatus.BodyName = currentStatusData.Body;
                currentStatus.PlanetRadius = currentStatusData.BodyRadius;
                currentStatus.Balance = currentStatusData.Balance;
                currentStatus.DestinationSystem = currentStatusData.Destination.SystemId;
                currentStatus.DestinationBody = currentStatusData.Destination.BodyId;
                currentStatus.DestinationName = currentStatusData.Destination.Name;
                currentStatus.Oxygen = currentStatusData.Oxygen;
                currentStatus.Health = currentStatusData.Health;
                currentStatus.Temperature = currentStatusData.Temperature;
                currentStatus.SelectedWeapon = currentStatusData.SelectedWeapon.ToString();
                currentStatus.Gravity = currentStatusData.Gravity;
            }
            CoreServer.GameDataEvent(GameEventType.Status, currentStatus);
        }

        public void HandleShipTargetedEvent(EliteAPI.Events.ShipTargetedEvent currentTargetData, EventContext context) {

            Log.Instance.Info("Handling ShipTargetedEvent");
            Log.Instance.Info("Targetlock: {istargetlocked}, Scanstage: {scanstage}", currentTargetData.IsTargetLocked.ToString(), currentTargetData.ScanStage.ToString());
            ShipTargetedData newTargetData = new ShipTargetedData();

            TimeSpan dataAge = DateTime.UtcNow.Subtract(currentTargetData.Timestamp);
            Log.Instance.Info("Target data age: {targetage}", dataAge.ToString());
            if (dataAge.TotalMinutes < 5) {
                newTargetData.LastUpdate = currentTargetData.Timestamp;
                newTargetData.TargetLocked = currentTargetData.IsTargetLocked;
                if (newTargetData.TargetLocked == true) {

                    // Ensure ship name starts with a Uppercase letter to look nice for nonlocalised ships
                    newTargetData.Ship = char.ToUpper(currentTargetData.Ship.ToString()[0]) + currentTargetData.Ship.ToString().Substring(1);

                    // In ELiteAPI Scanstage is a long, in ELiteFIPProtocl it's an int.
                    newTargetData.ScanStage = (int)currentTargetData.ScanStage;
                    if (newTargetData.ScanStage >= 1) {
                        if (String.IsNullOrEmpty(currentTargetData.PilotName.ToString())) {
                            newTargetData.PilotName = currentTargetData.PilotName.Symbol;
                        } else {
                            newTargetData.PilotName = currentTargetData.PilotName.ToString();
                        }
                        newTargetData.PilotRank = currentTargetData.PilotRank;
                    }
                    if (newTargetData.ScanStage >= 2) {
                        newTargetData.ShieldHealth = currentTargetData.ShieldHealth;
                        newTargetData.HullHealth = currentTargetData.HullHealth;
                    }
                    if (newTargetData.ScanStage >= 3) {
                        newTargetData.Faction = currentTargetData.Faction;
                        newTargetData.LegalStatus = currentTargetData.LegalStatus;
                        newTargetData.SubSystemHealth = currentTargetData.SubsystemHealth;
                        newTargetData.Bounty = currentTargetData.Bounty;
                        if (String.IsNullOrEmpty(currentTargetData.Subsystem.ToString())) {
                            newTargetData.SubSystem = currentTargetData.Subsystem.Symbol;
                        } else {
                            newTargetData.SubSystem = currentTargetData.Subsystem.ToString();
                        }
                    }
                }
                currentTarget = newTargetData;

                CoreServer.GameDataEvent(GameEventType.Target, currentTarget);
            }
        }

        public void HandleLocationEvent(EliteAPI.Events.LocationEvent currentLocationData, EventContext context) {

            Log.Instance.Info("Handling Location Event");

            currentLocation.LastUpdate = DateTime.Now;
            currentLocation.SystemId = currentLocationData.SystemAddress;
            currentLocation.SystemName = currentLocationData.StarSystem;
            currentLocation.BodyId = currentLocationData.BodyId;
            currentLocation.BodyName =  currentLocationData.Body;
            currentLocation.MarketId = currentLocationData.MarketId;
            currentLocation.StationName = currentLocationData.StationName;
            currentLocation.StationType = currentLocationData.StationType;

            CoreServer.GameDataEvent(GameEventType.Location, currentLocation);

        }
        public void HandleStartJumpEvent(EliteAPI.Events.StartJumpEvent startJumpData, EventContext context) {

            Log.Instance.Info("Handling StartJumpEvent Event");

            currentJump.LastUpdate = DateTime.Now;
            currentJump.JumpComplete = false;
            currentJump.OriginSystemId = currentLocation.SystemId;
            currentJump.OriginSystemName = currentLocation.SystemName;
            currentJump.DestinationSystemId = startJumpData.SystemAddress;
            currentJump.DestinationSystemName = startJumpData.StarSystem;
            currentJump.DestinationSystemClass = startJumpData.StarClass;
            currentJump.JumpDistance = 0;
            currentJump.FuelUsed = 0;               

            CoreServer.GameDataEvent(GameEventType.Jump, currentJump);

        }
        public void HandleFsdJumpEvent(EliteAPI.Events.FsdJumpEvent fsdJumpdataData, EventContext context) {

            Log.Instance.Info("Handling FsdJumpEvent Event");

            currentLocation.LastUpdate = DateTime.Now;
            currentLocation.SystemId = fsdJumpdataData.SystemAddress;
            currentLocation.SystemName = fsdJumpdataData.StarSystem;
            currentLocation.BodyId = fsdJumpdataData.BodyId;
            currentLocation.BodyName = fsdJumpdataData.Body;

            currentJump.DestinationSystemId = fsdJumpdataData.SystemAddress;
            currentJump.DestinationSystemName = fsdJumpdataData.StarSystem;
            currentJump.JumpDistance = fsdJumpdataData.JumpDist;
            currentJump.FuelUsed = fsdJumpdataData.FuelUsed;
            currentJump.JumpComplete = true;

            CoreServer.GameDataEvent(GameEventType.Location, currentLocation);
            CoreServer.GameDataEvent(GameEventType.Jump, currentJump);
        }

        public void HandleNavRouteEvent(EliteAPI.Events.Status.NavRoute.NavRouteEvent currentNavRouteData, EventContext context) {

            Log.Instance.Info("Handling NavRoute Event");

            currentNavRoute.LastUpdate = DateTime.Now;
            if ((currentNavRouteData.Stops != null) && (currentNavRouteData.Stops.Count() != 0)) {
                currentNavRoute.NavRouteActive = true;
                foreach (EliteAPI.Events.Status.NavRoute.NavRouteStop navRouteStop in currentNavRouteData.Stops) {
                    NavigationData.NavRouteStop navStop = new NavigationData.NavRouteStop();
                    navStop.SystemId = navRouteStop.Address;
                    navStop.SystemName = navRouteStop.System;
                    navStop.Class = navRouteStop.Class;
                    currentNavRoute.Stops.Add(navStop);
                }
            } else {
                currentNavRoute.NavRouteActive = false;
                currentNavRoute.Stops.Clear();
            }
            CoreServer.GameDataEvent(GameEventType.Navigation, currentNavRoute);
        }

        public void HandleNavRouteClearEvent(EliteAPI.Events.Status.NavRoute.NavRouteClearEvent navRouteClear, EventContext context) {

            Log.Instance.Info("Handling NavRouteClear Event");

            currentNavRoute.LastUpdate = DateTime.Now;
            currentNavRoute.NavRouteActive = false;
            currentNavRoute.Stops.Clear();

            CoreServer.GameDataEvent(GameEventType.Navigation, currentNavRoute);

        }

        public void HandleApproachBodyEvent(EliteAPI.Events.ApproachBodyEvent approachBodyData, EventContext context) {

            Log.Instance.Info("Handling ApproachBodyEvent Event");

            currentLocation.LastUpdate = DateTime.Now;
            currentLocation.BodyId = approachBodyData.BodyId;
            currentLocation.BodyName = approachBodyData.Body;

            CoreServer.GameDataEvent(GameEventType.Location, currentLocation);
        }

        public void HandleLeaveBodyEvent(EliteAPI.Events.LeaveBodyEvent leaveBodyData, EventContext context) {

            Log.Instance.Info("Handling LeaveBodyEvent Event");

            currentLocation.LastUpdate = DateTime.Now;
            currentLocation.BodyId = "";
            currentLocation.BodyName = "";

            CoreServer.GameDataEvent(GameEventType.Location, currentLocation);
        }

        public void HandleDockedEvent(EliteAPI.Events.DockedEvent dockedData, EventContext context) {

            Log.Instance.Info("Handling DockedEvent Event");

            currentLocation.LastUpdate = DateTime.Now;
            currentLocation.MarketId = dockedData.MarketId;
            currentLocation.StationName = dockedData.StationName;
            currentLocation.StationType = dockedData.StationType;

            CoreServer.GameDataEvent(GameEventType.Location, currentLocation);
        }

        public void HandleUndockedEvent(EliteAPI.Events.UndockedEvent undockedData, EventContext context) {

            Log.Instance.Info("Handling DockedEvent Event");

            currentLocation.LastUpdate = DateTime.Now;
            currentLocation.MarketId = "";
            currentLocation.StationName = "";
            currentLocation.StationType = "";

            CoreServer.GameDataEvent(GameEventType.Location, currentLocation);
        }

    }
}
