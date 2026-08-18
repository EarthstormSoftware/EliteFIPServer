using EliteAPI;
using EliteAPI.Events;
using EliteAPI.Events.Game;
using EliteFIPProtocol;
using EliteFIPServer.Logging;
using Newtonsoft.Json;

namespace EliteFIPServer {
    public class EliteAPIIntegration {

        private CoreServer CoreServer;
        public ComponentState CurrentState { get; private set; }

        // Game State Provider
        private EliteDangerousApi EliteAPI;

        // Current Game State Information
        private StatusData currentStatus = new StatusData();
        private ShipTargetedData currentTarget = new ShipTargetedData();
        private LocationData currentLocation = new LocationData();
        private NavigationData currentNavRoute = new NavigationData();
        private NavigationData previousNavRoute = new NavigationData();
        private JumpData currentJump = new JumpData();
        private ReceivedTextData currentReceivedText = new ReceivedTextData();

        public EliteAPIIntegration(CoreServer coreServer) {
            CoreServer = coreServer;
            CurrentState = new ComponentState();

            EliteAPI = new EliteDangerousApi();

            // Add events to watch list            
            EliteAPI.OnJson("Status", HandleStatusEvent);
            EliteAPI.On<ShipTargetedEvent>(HandleShipTargetedEvent);
            EliteAPI.On<LocationEvent>(HandleLocationEvent);
            EliteAPI.On<StartJumpEvent>(HandleStartJumpEvent);
            EliteAPI.On<FsdJumpEvent>(HandleFsdJumpEvent);
            EliteAPI.OnJson("NavRoute", HandleNavRouteEvent);
            EliteAPI.On<NavRouteClearEvent>(HandleNavRouteClearEvent);
            EliteAPI.On<ApproachBodyEvent>(HandleApproachBodyEvent);
            EliteAPI.On<LeaveBodyEvent>(HandleLeaveBodyEvent);
            EliteAPI.On<DockedEvent>(HandleDockedEvent);
            EliteAPI.On<UndockedEvent>(HandleUndockedEvent);
            EliteAPI.On<ReceiveTextEvent>(HandleReceiveTextEvent);
        }

        public void Start() {
            CurrentState.Set(RunState.Starting);
            // Start tracking game events
            EliteAPI.Start();
            CurrentState.Set(RunState.Started);
        }

        public void Stop() {
            CurrentState.Set(RunState.Stopping);
            // EliteAPI v5 exposes no Dispose()/Stop(); the old instance's file watchers keep
            // running in the background, so handlers below guard on IsRunning to ignore them.
            EliteAPI = new EliteDangerousApi();
            CurrentState.Set(RunState.Stopped);

        }

        private bool IsRunning => CurrentState.State == RunState.Started;


        public void FullClientUpdate() {
            CoreServer.GameDataEvent(GameEventType.Status, currentStatus);
            CoreServer.GameDataEvent(GameEventType.Target, currentTarget);
            CoreServer.GameDataEvent(GameEventType.Location, currentLocation);
            if (currentNavRoute.NavRouteActive) { CoreServer.GameDataEvent(GameEventType.Navigation, currentNavRoute); }
            CoreServer.GameDataEvent(GameEventType.PreviousNavRoute, previousNavRoute);
            CoreServer.GameDataEvent(GameEventType.Jump, currentJump);
            CoreServer.GameDataEvent(GameEventType.ReceivedText, currentReceivedText);

        }

        public void HandleStatusEvent((string eventName, string json) statusEvent) {
            if (!IsRunning) { return; }

            Log.Instance.Info("Handling Status Event");

            StatusJson currentStatusData = JsonConvert.DeserializeObject<StatusJson>(statusEvent.json);
            if (currentStatusData == null) { return; }

            currentStatus.LastUpdate = currentStatusData.Timestamp;

            // Original Status Flags
            if (currentStatusData.Available) {
                currentStatus.Docked = currentStatusData.GetShipFlag(ShipFlag.Docked);
                currentStatus.Landed = currentStatusData.GetShipFlag(ShipFlag.Landed);
                currentStatus.LandingGearDown = currentStatusData.GetShipFlag(ShipFlag.Gear);
                currentStatus.ShieldsUp = currentStatusData.GetShipFlag(ShipFlag.Shields);
                currentStatus.Supercruise = currentStatusData.GetShipFlag(ShipFlag.Supercruise);
                currentStatus.FlightAssistOff = currentStatusData.GetShipFlag(ShipFlag.FlightAssistOff);
                currentStatus.HardpointsDeployed = currentStatusData.GetShipFlag(ShipFlag.Hardpoints);
                currentStatus.InWing = currentStatusData.GetShipFlag(ShipFlag.Winging);
                currentStatus.LightsOn = currentStatusData.GetShipFlag(ShipFlag.Lights);
                currentStatus.CargoScoopDeployed = currentStatusData.GetShipFlag(ShipFlag.CargoScoop);
                currentStatus.SilentRunning = currentStatusData.GetShipFlag(ShipFlag.SilentRunning);
                currentStatus.ScoopingFuel = currentStatusData.GetShipFlag(ShipFlag.Scooping);
                currentStatus.SrvHandbrake = currentStatusData.GetShipFlag(ShipFlag.SrvHandbrake);
                currentStatus.SrvTurret = currentStatusData.GetShipFlag(ShipFlag.SrvTurret);
                currentStatus.SrvUnderShip = currentStatusData.GetShipFlag(ShipFlag.SrvNearShip);
                currentStatus.SrvDriveAssist = currentStatusData.GetShipFlag(ShipFlag.SrvDriveAssist);
                currentStatus.FsdMassLocked = currentStatusData.GetShipFlag(ShipFlag.MassLocked);
                currentStatus.FsdCharging = currentStatusData.GetShipFlag(ShipFlag.FsdCharging);
                currentStatus.FsdCooldown = currentStatusData.GetShipFlag(ShipFlag.FsdCooldown);
                currentStatus.LowFuel = currentStatusData.GetShipFlag(ShipFlag.LowFuel);
                currentStatus.Overheating = currentStatusData.GetShipFlag(ShipFlag.Overheating);
                if (currentStatusData.GetShipFlag(ShipFlag.HasLatLong)) {
                    currentStatus.HasLatLong = true;
                    currentStatus.Latitude = currentStatusData.Latitude;
                    currentStatus.Longitude = currentStatusData.Longitude;
                } else {
                    currentStatus.HasLatLong = false;
                    currentStatus.Latitude = 0;
                    currentStatus.Longitude = 0;
                }
                currentStatus.InDanger = currentStatusData.GetShipFlag(ShipFlag.InDanger);
                currentStatus.BeingInterdicted = currentStatusData.GetShipFlag(ShipFlag.InInterdiction);
                currentStatus.InMainShip = currentStatusData.GetShipFlag(ShipFlag.InMothership);
                currentStatus.InFighter = currentStatusData.GetShipFlag(ShipFlag.InFighter);
                currentStatus.InSRV = currentStatusData.GetShipFlag(ShipFlag.InSrv);
                currentStatus.HudAnalysisMode = currentStatusData.GetShipFlag(ShipFlag.AnalysisMode);
                currentStatus.NightVision = currentStatusData.GetShipFlag(ShipFlag.NightVision);
                currentStatus.AltitudeFromAverageRadius = currentStatusData.GetShipFlag(ShipFlag.AltitudeFromAverageRadius);
                currentStatus.FsdJump = currentStatusData.GetShipFlag(ShipFlag.FsdJump);
                currentStatus.SrvHighBeam = currentStatusData.GetShipFlag(ShipFlag.SrvHighBeam);

                currentStatus.OnFoot = currentStatusData.GetCommanderFlag(CommanderFlag.OnFoot);
                currentStatus.InTaxi = currentStatusData.GetCommanderFlag(CommanderFlag.InTaxi);
                currentStatus.InMulticrew = currentStatusData.GetCommanderFlag(CommanderFlag.InMultiCrew);
                currentStatus.OnFootInStation = currentStatusData.GetCommanderFlag(CommanderFlag.OnFootInStation);
                currentStatus.OnFootOnPlanet = currentStatusData.GetCommanderFlag(CommanderFlag.OnFootOnPlanet);
                currentStatus.AimDownSight = currentStatusData.GetCommanderFlag(CommanderFlag.AimDownSight);
                currentStatus.LowOxygen = currentStatusData.GetCommanderFlag(CommanderFlag.LowOxygen);
                currentStatus.LowHealth = currentStatusData.GetCommanderFlag(CommanderFlag.LowHealth);
                currentStatus.Cold = currentStatusData.GetCommanderFlag(CommanderFlag.Cold);
                currentStatus.Hot = currentStatusData.GetCommanderFlag(CommanderFlag.Hot);
                currentStatus.VeryCold = currentStatusData.GetCommanderFlag(CommanderFlag.VeryCold);
                currentStatus.VeryHot = currentStatusData.GetCommanderFlag(CommanderFlag.VeryHot);

                currentStatus.SystemPips = currentStatusData.Pips != null && currentStatusData.Pips.Length > 0 ? currentStatusData.Pips[0] : 0;
                currentStatus.EnginePips = currentStatusData.Pips != null && currentStatusData.Pips.Length > 1 ? currentStatusData.Pips[1] : 0;
                currentStatus.WeaponPips = currentStatusData.Pips != null && currentStatusData.Pips.Length > 2 ? currentStatusData.Pips[2] : 0;
                currentStatus.FireGroup = currentStatusData.FireGroup;
                currentStatus.GuiFocus = currentStatusData.GuiFocusName;
                currentStatus.FuelMain = currentStatusData.Fuel?.FuelMain ?? 0;
                currentStatus.FuelReservoir = currentStatusData.Fuel?.FuelReservoir ?? 0;
                currentStatus.Cargo = currentStatusData.Cargo;
                currentStatus.LegalState = currentStatusData.LegalState;
                currentStatus.Altitude = currentStatusData.Altitude;
                currentStatus.Heading = currentStatusData.Heading;
                currentStatus.BodyName = currentStatusData.BodyName;
                currentStatus.PlanetRadius = currentStatusData.PlanetRadius;
                currentStatus.Balance = currentStatusData.Balance;
                currentStatus.DestinationSystem = currentStatusData.Destination.SystemId;
                currentStatus.DestinationBody = currentStatusData.Destination.BodyId;
                currentStatus.DestinationName = currentStatusData.Destination.Name;
                currentStatus.Oxygen = currentStatusData.Oxygen;
                currentStatus.Health = currentStatusData.Health;
                currentStatus.Temperature = currentStatusData.Temperature;
                currentStatus.SelectedWeapon = Localisation.GetLocalisedString(currentStatusData.SelectedWeapon);
                currentStatus.Gravity = currentStatusData.Gravity;
            }
            CoreServer.GameDataEvent(GameEventType.Status, currentStatus);
        }

        public void HandleShipTargetedEvent(ShipTargetedEvent currentTargetData) {
            if (!IsRunning) { return; }

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

        public void HandleLocationEvent(LocationEvent currentLocationData) {
            if (!IsRunning) { return; }

            Log.Instance.Info("Handling Location Event");

            currentLocation.LastUpdate = currentLocationData.Timestamp;
            currentLocation.SystemId = currentLocationData.SystemAddress;
            currentLocation.SystemName = currentLocationData.StarSystem;
            currentLocation.BodyId = currentLocationData.BodyId;
            currentLocation.BodyName =  currentLocationData.Body;
            currentLocation.MarketId = currentLocationData.MarketId;
            currentLocation.StationName = currentLocationData.StationName;
            currentLocation.StationType = currentLocationData.StationType;

            CoreServer.GameDataEvent(GameEventType.Location, currentLocation);

        }
        public void HandleStartJumpEvent(StartJumpEvent startJumpData) {
            if (!IsRunning) { return; }

            Log.Instance.Info("Handling StartJumpEvent Event");            
            if (currentJump.LastUpdate <= startJumpData.Timestamp && startJumpData.JumpType == "Hyperspace") {
                currentJump.LastUpdate = startJumpData.Timestamp;
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


        }
        public void HandleFsdJumpEvent(FsdJumpEvent fsdJumpdataData) {
            if (!IsRunning) { return; }

            Log.Instance.Info("Handling FsdJumpEvent Event");

            if (currentLocation.LastUpdate <= fsdJumpdataData.Timestamp) {
                currentLocation.LastUpdate = fsdJumpdataData.Timestamp;
                currentLocation.SystemId = fsdJumpdataData.SystemAddress;
                currentLocation.SystemName = fsdJumpdataData.StarSystem;
                currentLocation.BodyId = fsdJumpdataData.BodyId;
                currentLocation.BodyName = fsdJumpdataData.Body;

                CoreServer.GameDataEvent(GameEventType.Location, currentLocation);
            }

            if (currentJump.LastUpdate <= fsdJumpdataData.Timestamp) {
                currentJump.LastUpdate = fsdJumpdataData.Timestamp;
                currentJump.DestinationSystemId = fsdJumpdataData.SystemAddress;
                currentJump.DestinationSystemName = fsdJumpdataData.StarSystem;
                currentJump.JumpDistance = fsdJumpdataData.JumpDist;
                currentJump.FuelUsed = fsdJumpdataData.FuelUsed;
                currentJump.JumpComplete = true;

                CoreServer.GameDataEvent(GameEventType.Jump, currentJump);
            }

            if (currentNavRoute.LastUpdate <= fsdJumpdataData.Timestamp && currentNavRoute.NavRouteActive && 
                currentNavRoute.Stops != null && currentNavRoute.Stops.Count() != 0) {

                foreach (NavigationData.NavRouteStop  navRouteStop in currentNavRoute.Stops) {
                    if (navRouteStop.SystemName == fsdJumpdataData.StarSystem) {
                        currentNavRoute.LastSystemReached = fsdJumpdataData.StarSystem;
                    }
                }
            }
        }

        public void HandleNavRouteEvent((string eventName, string json) navRouteEvent) {
            if (!IsRunning) { return; }

            Log.Instance.Info("Handling NavRoute Event");

            NavRouteJson currentNavRouteData = JsonConvert.DeserializeObject<NavRouteJson>(navRouteEvent.json);
            if (currentNavRouteData == null) { return; }

            Log.Instance.Info("Current data from: {curDataTime}, New data from: {newDataTime}", currentNavRoute.LastUpdate.ToString(), currentNavRouteData.Timestamp.ToString());
            if ((currentNavRoute.LastUpdate <= currentNavRouteData.Timestamp) && (currentNavRouteData.Route != null) && (currentNavRouteData.Route.Count() != 0))  {
                if (currentNavRoute.NavRouteActive) {
                    previousNavRoute = currentNavRoute.DeepCopy();
                    previousNavRoute.NavRouteActive = false;
                    CoreServer.GameDataEvent(GameEventType.PreviousNavRoute, previousNavRoute);
                }

                currentNavRoute.LastUpdate = currentNavRouteData.Timestamp;
                
                Log.Instance.Info("New route has {jumpcount} jumps", currentNavRouteData.Route.Count());
                currentNavRoute.NavRouteActive = true;
                currentNavRoute.Stops.Clear();
                foreach (NavRouteStopJson navRouteStop in currentNavRouteData.Route) {
                    NavigationData.NavRouteStop navStop = new NavigationData.NavRouteStop();
                    navStop.SystemId = navRouteStop.SystemAddress;
                    navStop.SystemName = navRouteStop.StarSystem;
                    navStop.Class = navRouteStop.StarClass;
                    currentNavRoute.Stops.Add(navStop);
                }
                CoreServer.GameDataEvent(GameEventType.Navigation, currentNavRoute);                
            }

        }

        public void HandleNavRouteClearEvent(NavRouteClearEvent navRouteClear) {
            if (!IsRunning) { return; }

            Log.Instance.Info("Handling NavRouteClear Event");
            Log.Instance.Info("Current data from: {curDataTime}, New data from: {newDataTime}", currentNavRoute.LastUpdate.ToString(), navRouteClear.Timestamp.ToString());
            if (currentNavRoute.LastUpdate <= navRouteClear.Timestamp) {
                if (currentNavRoute.NavRouteActive) {
                    previousNavRoute = currentNavRoute.DeepCopy();
                    previousNavRoute.NavRouteActive = false;
                    if (currentJump.JumpComplete == false) {
                        previousNavRoute.LastSystemReached = currentJump.DestinationSystemName;
                    }
                    CoreServer.GameDataEvent(GameEventType.PreviousNavRoute, previousNavRoute);
                }

                currentNavRoute.LastUpdate = navRouteClear.Timestamp;
                currentNavRoute.NavRouteActive = false;
                currentNavRoute.Stops.Clear();

                CoreServer.GameDataEvent(GameEventType.Navigation, currentNavRoute);                
            }

        }

        public void HandleApproachBodyEvent(ApproachBodyEvent approachBodyData) {
            if (!IsRunning) { return; }

            Log.Instance.Info("Handling ApproachBodyEvent Event");

            currentLocation.LastUpdate = approachBodyData.Timestamp;
            currentLocation.BodyId = approachBodyData.BodyId;
            currentLocation.BodyName = approachBodyData.Body;

            CoreServer.GameDataEvent(GameEventType.Location, currentLocation);
        }

        public void HandleLeaveBodyEvent(LeaveBodyEvent leaveBodyData) {
            if (!IsRunning) { return; }

            Log.Instance.Info("Handling LeaveBodyEvent Event");

            currentLocation.LastUpdate = leaveBodyData.Timestamp;
            currentLocation.BodyId = "";
            currentLocation.BodyName = "";

            CoreServer.GameDataEvent(GameEventType.Location, currentLocation);
        }

        public void HandleDockedEvent(DockedEvent dockedData) {
            if (!IsRunning) { return; }

            Log.Instance.Info("Handling DockedEvent Event");

            currentLocation.LastUpdate = dockedData.Timestamp;
            currentLocation.MarketId = dockedData.MarketId;
            currentLocation.StationName = dockedData.StationName;
            currentLocation.StationType = dockedData.StationType;

            CoreServer.GameDataEvent(GameEventType.Location, currentLocation);
        }

        public void HandleUndockedEvent(UndockedEvent undockedData) {
            if (!IsRunning) { return; }

            Log.Instance.Info("Handling UndockedEvent Event");

            currentLocation.LastUpdate = undockedData.Timestamp;
            currentLocation.MarketId = "";
            currentLocation.StationName = "";
            currentLocation.StationType = "";

            CoreServer.GameDataEvent(GameEventType.Location, currentLocation);
        }

        public void HandleReceiveTextEvent(ReceiveTextEvent receiveTextData) {
            if (!IsRunning) { return; }

            Log.Instance.Info("Handling ReceiveTextEvent Event");

            currentReceivedText.LastUpdate = receiveTextData.Timestamp;
            currentReceivedText.Channel = receiveTextData.Channel;
            currentReceivedText.Source = receiveTextData.From.ToString();
            currentReceivedText.Message = receiveTextData.Message.ToString();

            CoreServer.GameDataEvent(GameEventType.ReceivedText, currentReceivedText);
        }
    }
}
