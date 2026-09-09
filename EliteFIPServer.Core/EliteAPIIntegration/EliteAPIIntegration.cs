using EliteAPI;
using EliteAPI.Events;
using EliteAPI.Events.Game;
using EliteAPI.Journals;
using EliteFIPProtocol;
using EliteFIPServer.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        private RouteTargetData currentRouteTarget = new RouteTargetData();
        private ReceivedTextData currentReceivedText = new ReceivedTextData();
        private StationData currentStation = new StationData();
        private ExplorationData currentExploration = new ExplorationData();
        private LoadoutData currentLoadout = new LoadoutData();
        private MissionData currentMission = new MissionData();
        private MissionCollectionData currentMissions = new MissionCollectionData();
        private DockingData currentDocking = new DockingData();
        private CargoData currentCargo = new CargoData();
        private MaterialsData currentMaterials = new MaterialsData();
        private SystemData currentSystem = new SystemData();
        private readonly string persistedRoutePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EliteFIPServer",
            "PanelRoute.json");

        public EliteAPIIntegration(CoreServer coreServer) {
            CoreServer = coreServer;
            CurrentState = new ComponentState();
            currentNavRoute = LoadPersistedRoute();

            EliteAPI = new EliteDangerousApi();

            // Add events to watch list            
            EliteAPI.OnJson("Status", HandleStatusEvent);
            EliteAPI.On<ShipTargetedEvent>(HandleShipTargetedEvent);
            EliteAPI.On<LocationEvent>(HandleLocationEvent);
            EliteAPI.On<StartJumpEvent>(HandleStartJumpEvent);
            EliteAPI.On<FsdJumpEvent>(HandleFsdJumpEvent);
            EliteAPI.On<FsdTargetEvent>(HandleFsdTargetEvent);
            EliteAPI.OnJson("NavRoute", HandleNavRouteEvent);
            EliteAPI.On<NavRouteClearEvent>(HandleNavRouteClearEvent);
            EliteAPI.On<ApproachBodyEvent>(HandleApproachBodyEvent);
            EliteAPI.On<LeaveBodyEvent>(HandleLeaveBodyEvent);
            EliteAPI.On<DockedEvent>(HandleDockedEvent);
            EliteAPI.On<UndockedEvent>(HandleUndockedEvent);
            EliteAPI.On<DockingRequestedEvent>(HandleDockingRequestedEvent);
            EliteAPI.On<DockingGrantedEvent>(HandleDockingGrantedEvent);
            EliteAPI.On<DockingDeniedEvent>(HandleDockingDeniedEvent);
            EliteAPI.On<DockingCancelledEvent>(HandleDockingCancelledEvent);
            EliteAPI.On<DockingTimeoutEvent>(HandleDockingTimeoutEvent);
            EliteAPI.On<ReceiveTextEvent>(HandleReceiveTextEvent);
            EliteAPI.OnJson("Cargo", HandleCargoJson);
            EliteAPI.On<ScanEvent>(HandleScanEvent);
            EliteAPI.On<LoadoutEvent>(HandleLoadoutEvent);
            EliteAPI.On<MissionAcceptedEvent>(HandleMissionAcceptedEvent);
            EliteAPI.On<MissionsEvent>(HandleMissionsEvent);
            EliteAPI.On<MissionCompletedEvent>(HandleMissionCompletedEvent);
            EliteAPI.On<MissionFailedEvent>(HandleMissionFailedEvent);
            EliteAPI.On<MissionAbandonedEvent>(HandleMissionAbandonedEvent);
            EliteAPI.On<MaterialsEvent>(HandleMaterialsEvent);
            EliteAPI.On<MaterialCollectedEvent>(HandleMaterialCollectedEvent);
            EliteAPI.On<MaterialDiscardedEvent>(HandleMaterialDiscardedEvent);
            EliteAPI.On<MaterialTradeEvent>(HandleMaterialTradeEvent);
            EliteAPI.On<CargoTransferEvent>(HandleCargoTransferEvent);
            EliteAPI.On<ShieldStateEvent>(HandleShieldStateEvent);
            EliteAPI.On<HullDamageEvent>(HandleHullDamageEvent);
            EliteAPI.On<UnderAttackEvent>(HandleUnderAttackEvent);
            EliteAPI.On<BountyEvent>(HandleBountyEvent);
            EliteAPI.On<InterdictedEvent>(HandleInterdictedEvent);
            EliteAPI.On<InterdictionEvent>(HandleInterdictionEvent);
        }

        public void Start() {
            CurrentState.Set(RunState.Starting);
            // Start tracking game events
            EliteAPI.Start();
            CurrentState.Set(RunState.Started);
            HydrateCurrentState();
        }

        private void HydrateCurrentState() {
            try {
                var journalsDirectory = JournalUtils.GetJournalsDirectory();
                var statusFile = Path.Combine(journalsDirectory.FullName, "Status.json");
                if (File.Exists(statusFile)) {
                    HandleStatusEvent(("Status", File.ReadAllText(statusFile)));
                }

                var journalFile = journalsDirectory.GetFiles("Journal.*.log")
                    .OrderByDescending(file => file.LastWriteTimeUtc)
                    .FirstOrDefault();
                if (journalFile == null) { return; }

                var journalLines = ReadJournalLines(journalFile.FullName);
                var stateEvents = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                    "Location", "Docked", "Undocked", "FSDJump", "ApproachBody", "LeaveBody"
                };
                var latestStateEvent = journalLines
                    .Reverse()
                    .Select(line => (Line: line, EventName: GetEventName(line)))
                    .FirstOrDefault(item => stateEvents.Contains(item.EventName));

                if (!string.IsNullOrEmpty(latestStateEvent.Line)) {
                    InvokeHydratedJournalLine(latestStateEvent.Line);
                }

                var latestLoadoutEvent = journalLines
                    .Reverse()
                    .Select(line => (Line: line, EventName: GetEventName(line)))
                    .FirstOrDefault(item => string.Equals(item.EventName, "Loadout", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(latestLoadoutEvent.Line)) {
                    InvokeHydratedJournalLine(latestLoadoutEvent.Line);
                }

                var latestMaterialsEvent = journalLines
                    .Reverse()
                    .Select(line => (Line: line, EventName: GetEventName(line)))
                    .FirstOrDefault(item => string.Equals(item.EventName, "Materials", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(latestMaterialsEvent.Line)) {
                    InvokeHydratedJournalLine(latestMaterialsEvent.Line);
                }

                var latestMissionEvent = journalLines
                    .Reverse()
                    .Select(line => (Line: line, EventName: GetEventName(line)))
                    .FirstOrDefault(item => string.Equals(item.EventName, "Missions", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(latestMissionEvent.Line)) {
                    InvokeHydratedJournalLine(latestMissionEvent.Line);
                }

                var cargoFile = Path.Combine(journalsDirectory.FullName, "Cargo.json");
                if (File.Exists(cargoFile)) {
                    HandleCargoJson(("Cargo", ReadSharedText(cargoFile)));
                }
            } catch (Exception ex) {
                Log.Instance.Warn("Initial Elite API state hydration failed: {exception}", ex.ToString());
            }
        }

        private void InvokeHydratedJournalLine(string json) {
            JournalUtils.PrepareLocalisations(json);
            EliteAPI.Invoke(json);
        }

        private static string[] ReadJournalLines(string path) {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            var lines = new List<string>();
            while (reader.ReadLine() is string line) {
                lines.Add(line);
            }
            return lines.ToArray();
        }

        private static string ReadSharedText(string path) {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private NavigationData LoadPersistedRoute() {
            try {
                if (!File.Exists(persistedRoutePath)) {
                    return new NavigationData();
                }

                var route = JsonConvert.DeserializeObject<NavigationData>(File.ReadAllText(persistedRoutePath));
                return route ?? new NavigationData();
            } catch (Exception ex) {
                Log.Instance.Warn("Persisted panel route could not be loaded: {exception}", ex.ToString());
                return new NavigationData();
            }
        }

        private void SavePersistedRoute() {
            try {
                var directory = Path.GetDirectoryName(persistedRoutePath);
                Directory.CreateDirectory(directory);
                var temporaryPath = persistedRoutePath + ".tmp";
                File.WriteAllText(temporaryPath, JsonConvert.SerializeObject(currentNavRoute));
                File.Move(temporaryPath, persistedRoutePath, true);
            } catch (Exception ex) {
                Log.Instance.Warn("Persisted panel route could not be saved: {exception}", ex.ToString());
            }
        }

        private static string GetEventName(string json) {
            try {
                return JObject.Parse(json)["event"]?.Value<string>();
            } catch (JsonException) {
                return null;
            }
        }

        public void Stop() {
            CurrentState.Set(RunState.Stopping);
            // EliteAPI v5 exposes no Dispose()/Stop(); the old instance's file watchers keep
            // running in the background, so handlers below guard on IsRunning to ignore them.
            EliteAPI = new EliteDangerousApi();
            CurrentState.Set(RunState.Stopped);

        }

        private bool IsRunning => CurrentState.State == RunState.Started;


        public Task FullClientUpdate(string connectionId) {
            return CoreServer.SendPanelSnapshot(connectionId, currentStatus, currentTarget, currentLocation,
                currentNavRoute, previousNavRoute, currentJump, currentRouteTarget, currentReceivedText, currentStation,
                currentExploration, currentLoadout, currentMission, currentMissions, currentDocking,
                currentCargo, currentMaterials, currentSystem);
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
                    newTargetData.Ship = JournalDisplayText.Format(currentTargetData.Ship.ToString());

                    // In ELiteAPI Scanstage is a long, in ELiteFIPProtocl it's an int.
                    newTargetData.ScanStage = (int)currentTargetData.ScanStage;
                    if (newTargetData.ScanStage >= 1) {
                        if (String.IsNullOrEmpty(currentTargetData.PilotName.ToString())) {
                            newTargetData.PilotName = JournalDisplayText.Format(currentTargetData.PilotName.Symbol);
                        } else {
                            newTargetData.PilotName = JournalDisplayText.Format(currentTargetData.PilotName.ToString());
                        }
                        newTargetData.PilotRank = currentTargetData.PilotRank;
                        newTargetData.SquadronId = currentTargetData.SquadronId;
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
                        newTargetData.Power = currentTargetData.Power;
                        if (String.IsNullOrEmpty(currentTargetData.Subsystem.ToString())) {
                            newTargetData.SubSystem = JournalDisplayText.Format(currentTargetData.Subsystem.Symbol);
                        } else {
                            newTargetData.SubSystem = JournalDisplayText.Format(currentTargetData.Subsystem.ToString());
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
            currentLocation.Latitude = currentLocationData.Latitude;
            currentLocation.Longitude = currentLocationData.Longitude;
            currentLocation.DistanceFromStarInLightSeconds = currentLocationData.DistanceFromStarInLightSeconds;
            currentLocation.IsDocked = currentLocationData.IsDocked;
            currentLocation.IsInTaxi = currentLocationData.IsInTaxi;
            currentLocation.IsInMultiCrew = currentLocationData.IsInMultiCrew;
            currentLocation.IsInSrv = currentLocationData.IsInSrv;
            currentLocation.IsOnFoot = currentLocationData.IsOnFoot;

            currentExploration = new ExplorationData {
                LastUpdate = currentLocationData.Timestamp,
                SystemId = currentLocationData.SystemAddress,
                SystemName = currentLocationData.StarSystem,
                BodyId = currentLocationData.BodyId,
                BodyName = currentLocationData.Body,
                BodyType = currentLocationData.BodyType
            };
            currentStation = CreateStationData(currentLocationData);
            currentSystem = CreateSystemData(currentLocationData);

            CoreServer.GameDataEvent(GameEventType.Location, currentLocation);
            CoreServer.GameDataEvent(GameEventType.Station, currentStation);
            CoreServer.GameDataEvent(GameEventType.Exploration, currentExploration);
            CoreServer.GameDataEvent(GameEventType.System, currentSystem);

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
                currentJump.FuelLevel = fsdJumpdataData.FuelLevel;
                currentJump.BoostUsed = fsdJumpdataData.BoostUsed;
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

        public void HandleFsdTargetEvent(FsdTargetEvent targetData) {
            if (!IsRunning) { return; }

            currentRouteTarget = new RouteTargetData {
                LastUpdate = targetData.Timestamp,
                SystemId = targetData.SystemAddress,
                SystemName = targetData.Name,
                StarClass = targetData.StarClass,
                RemainingJumps = targetData.RemainingJumpsInRoute
            };
            CoreServer.GameDataEvent(GameEventType.RouteTarget, currentRouteTarget);
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
                SavePersistedRoute();
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
                SavePersistedRoute();

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
            currentLocation.SystemId = dockedData.SystemAddress;
            currentLocation.SystemName = dockedData.StarSystem;
            currentLocation.MarketId = dockedData.MarketId;
            currentLocation.StationName = dockedData.StationName;
            currentLocation.StationType = dockedData.StationType;

            currentStation = new StationData {
                LastUpdate = dockedData.Timestamp,
                SystemId = dockedData.SystemAddress,
                SystemName = dockedData.StarSystem,
                StationName = dockedData.StationName,
                StationType = dockedData.StationType,
                MarketId = dockedData.MarketId,
                Faction = dockedData.StationFaction.Name,
                Government = JournalDisplayText.Format(dockedData.StationGovernment.ToString()),
                Allegiance = dockedData.StationAllegiance,
                Economy = JournalDisplayText.Format(dockedData.StationEconomy.ToString()),
                Services = dockedData.StationServices?.Select(JournalDisplayText.Format).Distinct().ToArray() ?? Array.Empty<string>(),
                DistanceFromStarInLightSeconds = dockedData.DistanceFromStarInLightSeconds,
                IsDocked = true,
                HasActiveFine = dockedData.HasActiveFine,
                IsWanted = dockedData.IsWanted,
                HasBreachedCockpit = dockedData.HasBreachedCockpit,
                StationState = dockedData.StationState,
                SmallLandingPads = dockedData.LandingPads.Small,
                MediumLandingPads = dockedData.LandingPads.Medium,
                LargeLandingPads = dockedData.LandingPads.Large
            };

            CoreServer.GameDataEvent(GameEventType.Location, currentLocation);
            CoreServer.GameDataEvent(GameEventType.Station, currentStation);
            PublishDocking(dockedData.Timestamp, "Docked", dockedData.StationName, dockedData.StationType, dockedData.MarketId);
        }

        public void HandleUndockedEvent(UndockedEvent undockedData) {
            if (!IsRunning) { return; }

            Log.Instance.Info("Handling UndockedEvent Event");

            currentLocation.LastUpdate = undockedData.Timestamp;
            currentLocation.MarketId = "";
            currentLocation.StationName = "";
            currentLocation.StationType = "";
            currentStation = new StationData {
                LastUpdate = undockedData.Timestamp,
                SystemId = currentLocation.SystemId,
                SystemName = currentLocation.SystemName,
                IsDocked = false
            };

            CoreServer.GameDataEvent(GameEventType.Location, currentLocation);
            CoreServer.GameDataEvent(GameEventType.Station, currentStation);
            PublishDocking(undockedData.Timestamp, "Undocked", "", "", undockedData.MarketId);
        }

        public void HandleDockingRequestedEvent(DockingRequestedEvent data) => PublishDocking(
            data.Timestamp, "Requested", data.StationName, data.StationType, data.MarketId);

        public void HandleDockingGrantedEvent(DockingGrantedEvent data) => PublishDocking(
            data.Timestamp, "Granted", data.StationName, data.StationType, data.MarketId, data.LandingPad);

        public void HandleDockingDeniedEvent(DockingDeniedEvent data) => PublishDocking(
            data.Timestamp, "Denied", data.StationName, data.StationType, data.MarketId, reason: data.Reason);

        public void HandleDockingCancelledEvent(DockingCancelledEvent data) => PublishDocking(
            data.Timestamp, "Cancelled", data.StationName, data.StationType, data.MarketID);

        public void HandleDockingTimeoutEvent(DockingTimeoutEvent data) => PublishDocking(
            data.Timestamp, "Timed out", data.StationName, data.StationType, data.MarketID);

        private void PublishDocking(DateTime timestamp, string status, string stationName, string stationType,
            string marketId, long landingPad = 0, string reason = null) {
            if (!IsRunning) { return; }
            currentDocking = new DockingData {
                LastUpdate = timestamp,
                Status = status,
                StationName = stationName,
                StationType = stationType,
                MarketId = marketId,
                LandingPad = landingPad,
                Reason = reason
            };
            CoreServer.GameDataEvent(GameEventType.Docking, currentDocking);
        }

        public void HandleReceiveTextEvent(ReceiveTextEvent receiveTextData) {
            if (!IsRunning) { return; }

            Log.Instance.Info("Handling ReceiveTextEvent Event");

            currentReceivedText.LastUpdate = receiveTextData.Timestamp;
            currentReceivedText.Channel = receiveTextData.Channel;
            currentReceivedText.Source = JournalDisplayText.Format(receiveTextData.From.ToString());
            currentReceivedText.Message = receiveTextData.Message.ToString();

            CoreServer.GameDataEvent(GameEventType.ReceivedText, currentReceivedText);
        }

        public void HandleScanEvent(ScanEvent scanData) {
            if (!IsRunning) { return; }

            currentExploration = new ExplorationData {
                LastUpdate = scanData.Timestamp,
                SystemId = scanData.SystemAddress,
                SystemName = scanData.StarSystem,
                BodyId = scanData.BodyId,
                BodyName = scanData.BodyName,
                BodyType = scanData.ScanType,
                StarType = scanData.StarType,
                PlanetClass = scanData.PlanetClass,
                Atmosphere = scanData.Atmosphere,
                SurfaceGravity = scanData.SurfaceGravity,
                SurfaceTemperature = scanData.SurfaceTemperature,
                DistanceFromArrivalLs = scanData.DistanceFromArrivalLs,
                IsLandable = scanData.IsLandable,
                WasDiscovered = scanData.WasDiscovered,
                WasMapped = scanData.WasMapped,
                TerraformState = scanData.TerraformState,
                AtmosphereType = scanData.AtmosphereType,
                Volcanism = scanData.Volcanism,
                SurfacePressure = scanData.SurfacePressure,
                Radius = scanData.Radius,
                StellarMass = scanData.StellarMass,
                Luminosity = scanData.Luminosity,
                ReserveLevel = scanData.ReserveLevel,
                Materials = scanData.Materials?.Select(material => new BodyMaterialData {
                    Name = material.Name,
                    Percent = material.Percent
                }).ToArray() ?? Array.Empty<BodyMaterialData>(),
                Rings = scanData.Rings?.Select(ring => new BodyRingData {
                    Name = ring.Name,
                    RingClass = ring.RingClass
                }).ToArray() ?? Array.Empty<BodyRingData>()
            };
            CoreServer.GameDataEvent(GameEventType.Exploration, currentExploration);
        }

        public void HandleLoadoutEvent(LoadoutEvent loadoutData) {
            if (!IsRunning) { return; }

            currentLoadout = new LoadoutData {
                LastUpdate = loadoutData.Timestamp,
                Ship = loadoutData.Ship,
                ShipId = loadoutData.ShipId,
                ShipName = loadoutData.ShipName,
                ShipIdent = loadoutData.ShipIdent,
                HullHealth = loadoutData.HullHealth,
                CargoCapacity = loadoutData.CargoCapacity,
                MaxJumpRange = loadoutData.MaxJumpRange,
                MainFuelCapacity = loadoutData.FuelCapacity.Main,
                ReserveFuelCapacity = loadoutData.FuelCapacity.Reserve,
                Rebuy = loadoutData.Rebuy,
                Modules = loadoutData.Modules?.Select(module => new LoadoutModuleData {
                    Slot = JournalDisplayText.Format(module.Slot),
                    Item = JournalDisplayText.Format(module.Item),
                    IsOn = module.IsOn,
                    Priority = module.Priority,
                    Value = module.Value,
                    Health = module.Health,
                    AmmoInClip = module.AmmoInClip,
                    AmmoInHopper = module.AmmoInHopper,
                    Engineering = string.IsNullOrEmpty(module.Engineering.BlueprintName) ? null : new ModuleEngineeringData {
                        Engineer = module.Engineering.Engineer,
                        BlueprintName = JournalDisplayText.Format(module.Engineering.BlueprintName),
                        ExperimentalEffect = JournalDisplayText.Format(module.Engineering.ExperimentalEffect.ToString()),
                        Level = module.Engineering.Level,
                        Quality = module.Engineering.Quality,
                        Modifiers = module.Engineering.Modifications?.Select(modifier => new ModuleModifierData {
                            Label = modifier.Label,
                            Value = modifier.Value,
                            OriginalValue = modifier.OriginalValue,
                            LessIsGood = modifier.LessIsGood != 0
                        }).ToArray() ?? Array.Empty<ModuleModifierData>()
                    }
                }).ToArray() ?? Array.Empty<LoadoutModuleData>()
            };
            CoreServer.GameDataEvent(GameEventType.Loadout, currentLoadout);
        }

        public void HandleMissionAcceptedEvent(MissionAcceptedEvent missionData) {
            if (!IsRunning) { return; }

            currentMission = new MissionData {
                LastUpdate = missionData.Timestamp,
                MissionId = missionData.MissionId,
                Name = missionData.Name,
                LocalisedName = missionData.LocalisedName,
                Faction = missionData.Faction,
                Target = missionData.Target,
                TargetType = JournalDisplayText.Format(missionData.TargetType.ToString()),
                DestinationSystem = missionData.DestinationSystem,
                DestinationStation = missionData.DestinationStation,
                Expiry = missionData.Expiry,
                Reward = missionData.Reward,
                Count = missionData.Count,
                Commodity = missionData.Commodity,
                IsWing = missionData.IsWing
            };
            CoreServer.GameDataEvent(GameEventType.Mission, currentMission);
        }

        public void HandleMissionsEvent(MissionsEvent missionsData) {
            if (!IsRunning) { return; }
            currentMissions = new MissionCollectionData {
                LastUpdate = missionsData.Timestamp,
                Active = MapMissionSummaries(missionsData.Active),
                Failed = MapMissionSummaries(missionsData.Failed),
                Complete = MapMissionSummaries(missionsData.Complete)
            };
            CoreServer.GameDataEvent(GameEventType.MissionCollection, currentMissions);
        }

        private static IReadOnlyList<MissionSummaryData> MapMissionSummaries(
            IReadOnlyCollection<MissionsEvent.MissionInfo> missions) {
            return missions?.Select(mission => new MissionSummaryData {
                MissionId = mission.MissionId,
                Name = JournalDisplayText.Format(mission.Name),
                IsPassengerMission = mission.IsPassengerMission,
                ExpiresInSeconds = mission.Expires
            }).ToArray() ?? Array.Empty<MissionSummaryData>();
        }

        public void HandleMissionCompletedEvent(MissionCompletedEvent missionData) {
            if (!IsRunning) { return; }
            CoreServer.GameDataEvent(GameEventType.MissionLifecycle, new MissionLifecycleData {
                LastUpdate = missionData.Timestamp, Operation = "Completed", MissionId = missionData.MissionId,
                Name = missionData.Name, LocalisedName = missionData.LocalisedName, Reward = missionData.Reward
            });
        }

        public void HandleMissionFailedEvent(MissionFailedEvent missionData) {
            if (!IsRunning) { return; }
            CoreServer.GameDataEvent(GameEventType.MissionLifecycle, new MissionLifecycleData {
                LastUpdate = missionData.Timestamp, Operation = "Failed", MissionId = missionData.MissionId,
                Name = missionData.Name, LocalisedName = missionData.LocalisedName, Fine = missionData.Fine
            });
        }

        public void HandleMissionAbandonedEvent(MissionAbandonedEvent missionData) {
            if (!IsRunning) { return; }
            CoreServer.GameDataEvent(GameEventType.MissionLifecycle, new MissionLifecycleData {
                LastUpdate = missionData.Timestamp, Operation = "Abandoned", MissionId = missionData.MissionId,
                Name = missionData.Name, LocalisedName = missionData.LocalisedName
            });
        }

        public void HandleMaterialsEvent(MaterialsEvent materialsData) {
            if (!IsRunning) { return; }
            currentMaterials = new MaterialsData {
                LastUpdate = materialsData.Timestamp,
                Raw = materialsData.Raw?.Select(item => new MaterialItemData { Name = JournalDisplayText.Format(item.Name), Count = item.Count, Category = "Raw" }).ToArray() ?? Array.Empty<MaterialItemData>(),
                Manufactured = materialsData.Manufactured?.Select(item => new MaterialItemData { Name = JournalDisplayText.Format(item.Name.ToString()), Count = item.Count, Category = "Manufactured" }).ToArray() ?? Array.Empty<MaterialItemData>(),
                Encoded = materialsData.Encoded?.Select(item => new MaterialItemData { Name = JournalDisplayText.Format(item.Name.ToString()), Count = item.Count, Category = "Encoded" }).ToArray() ?? Array.Empty<MaterialItemData>()
            };
            CoreServer.GameDataEvent(GameEventType.Materials, currentMaterials);
        }

        public void HandleMaterialCollectedEvent(MaterialCollectedEvent data) {
            UpdateMaterial(data.Timestamp, data.Category, JournalDisplayText.Format(data.Name.ToString()), data.Count);
        }

        public void HandleMaterialDiscardedEvent(MaterialDiscardedEvent data) {
            UpdateMaterial(data.Timestamp, data.Category, JournalDisplayText.Format(data.Name), -data.Count);
        }

        public void HandleMaterialTradeEvent(MaterialTradeEvent data) {
            UpdateMaterial(data.Timestamp, data.Paid.Category, JournalDisplayText.Format(data.Paid.Material.ToString()), -data.Paid.Quantity, false);
            UpdateMaterial(data.Timestamp, data.Received.Category, JournalDisplayText.Format(data.Received.Material.ToString()), data.Received.Quantity);
        }

        private void UpdateMaterial(DateTime timestamp, string category, string name, long delta, bool publish = true) {
            IReadOnlyList<MaterialItemData> Update(IReadOnlyList<MaterialItemData> source) {
                var items = source.ToList();
                var index = items.FindIndex(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
                var count = Math.Max(0, (index >= 0 ? items[index].Count : 0) + delta);
                if (index >= 0) { items.RemoveAt(index); }
                if (count > 0) { items.Add(new MaterialItemData { Name = name, Count = count, Category = category }); }
                return items;
            }

            currentMaterials = new MaterialsData {
                LastUpdate = timestamp,
                Raw = string.Equals(category, "Raw", StringComparison.OrdinalIgnoreCase) ? Update(currentMaterials.Raw) : currentMaterials.Raw,
                Manufactured = string.Equals(category, "Manufactured", StringComparison.OrdinalIgnoreCase) ? Update(currentMaterials.Manufactured) : currentMaterials.Manufactured,
                Encoded = string.Equals(category, "Encoded", StringComparison.OrdinalIgnoreCase) ? Update(currentMaterials.Encoded) : currentMaterials.Encoded
            };
            if (publish) { CoreServer.GameDataEvent(GameEventType.Materials, currentMaterials); }
        }

        public void HandleCargoTransferEvent(CargoTransferEvent cargoData) {
            if (!IsRunning) { return; }
            currentCargo = new CargoData {
                LastUpdate = cargoData.Timestamp,
                Items = cargoData.Transfers?.Select(item => new CargoItemData { Name = item.Type, Count = item.Count }).ToArray() ?? Array.Empty<CargoItemData>()
            };
            CoreServer.GameDataEvent(GameEventType.Cargo, currentCargo);
        }

        public void HandleCargoJson((string eventName, string json) cargoData) {
            if (!IsRunning) { return; }
            try {
                var json = JObject.Parse(cargoData.json);
                currentCargo = new CargoData {
                    LastUpdate = json["timestamp"]?.Value<DateTime>() ?? DateTime.UtcNow,
                    Capacity = json["CargoTotal"]?.Value<double>() ?? 0,
                    Total = json["Inventory"]?.Sum(item => item["Count"]?.Value<double>() ?? 0) ?? 0,
                    Items = json["Inventory"]?.Select(item => new CargoItemData {
                        Name = item["Name_Localised"]?.Value<string>() ?? JournalDisplayText.Format(item["Name"]?.Value<string>()),
                        Count = item["Count"]?.Value<long>() ?? 0,
                        IsStolen = item["Stolen"]?.Value<bool>() ?? false,
                        MissionId = item["MissionID"]?.Value<string>()
                    }).ToArray() ?? Array.Empty<CargoItemData>()
                };
                CoreServer.GameDataEvent(GameEventType.Cargo, currentCargo);
            } catch (JsonException) { }
        }

        public void HandleShieldStateEvent(ShieldStateEvent combatData) {
            if (!IsRunning) { return; }
            CoreServer.GameDataEvent(GameEventType.Combat, new CombatData {
                LastUpdate = combatData.Timestamp, Operation = "ShieldState", ShieldsUp = combatData.IsShieldsUp
            });
        }

        public void HandleHullDamageEvent(HullDamageEvent combatData) {
            if (!IsRunning) { return; }
            CoreServer.GameDataEvent(GameEventType.Combat, new CombatData {
                LastUpdate = combatData.Timestamp, Operation = "HullDamage", HullHealth = combatData.Health,
                IsPlayerPilot = combatData.IsPlayerPilot, IsFighter = combatData.IsFighter
            });
        }

        public void HandleUnderAttackEvent(UnderAttackEvent combatData) {
            if (!IsRunning) { return; }
            CoreServer.GameDataEvent(GameEventType.Combat, new CombatData {
                LastUpdate = combatData.Timestamp, Operation = "UnderAttack", Target = combatData.Target
            });
        }

        public void HandleBountyEvent(BountyEvent combatData) {
            if (!IsRunning) { return; }
            CoreServer.GameDataEvent(GameEventType.Combat, new CombatData {
                LastUpdate = combatData.Timestamp, Operation = "Bounty", Target = JournalDisplayText.Format(combatData.Target.ToString()),
                Reward = combatData.TotalReward, Faction = combatData.VictimFaction
            });
        }

        public void HandleInterdictedEvent(InterdictedEvent combatData) {
            if (!IsRunning) { return; }
            CoreServer.GameDataEvent(GameEventType.Combat, new CombatData {
                LastUpdate = combatData.Timestamp, Operation = "Interdicted", Target = JournalDisplayText.Format(combatData.Interdictor.ToString()),
                Faction = combatData.Faction, IsInterdiction = true, IsSuccessful = combatData.HasSubmitted
            });
        }

        public void HandleInterdictionEvent(InterdictionEvent combatData) {
            if (!IsRunning) { return; }
            CoreServer.GameDataEvent(GameEventType.Combat, new CombatData {
                LastUpdate = combatData.Timestamp, Operation = "Interdiction", Faction = combatData.Faction,
                IsInterdiction = true, IsSuccessful = combatData.IsSuccess
            });
        }

        private static StationData CreateStationData(LocationEvent locationData) {
            return new StationData {
                LastUpdate = locationData.Timestamp,
                SystemId = locationData.SystemAddress,
                SystemName = locationData.StarSystem,
                StationName = locationData.StationName,
                StationType = locationData.StationType,
                MarketId = locationData.MarketId,
                Faction = locationData.StationFaction.Name,
                Government = JournalDisplayText.Format(locationData.StationGovernment.ToString()),
                Allegiance = locationData.StationAllegiance,
                Economy = JournalDisplayText.Format(locationData.StationEconomy.ToString()),
                Services = locationData.StationServices?.Select(JournalDisplayText.Format).Distinct().ToArray() ?? Array.Empty<string>(),
                IsDocked = locationData.IsDocked,
                DistanceFromStarInLightSeconds = locationData.DistanceFromStarInLightSeconds
            };
        }

        private static SystemData CreateSystemData(LocationEvent locationData) {
            return new SystemData {
                LastUpdate = locationData.Timestamp,
                SystemId = locationData.SystemAddress,
                SystemName = locationData.StarSystem,
                SystemAllegiance = locationData.SystemAllegiance,
                Economy = JournalDisplayText.Format(locationData.SystemEconomy.ToString()),
                SecondaryEconomy = JournalDisplayText.Format(locationData.SystemSecondEconomy.ToString()),
                Government = JournalDisplayText.Format(locationData.SystemGovernment.ToString()),
                Security = JournalDisplayText.Format(locationData.SystemSecurity.ToString()),
                Population = locationData.Population,
                StarPosition = locationData.StarPos?.ToArray() ?? Array.Empty<double>(),
                Powers = locationData.Powers?.ToArray() ?? Array.Empty<string>(),
                PowerplayState = locationData.PowerplayState,
                ControllingFaction = locationData.SystemFaction.Name,
                Factions = locationData.Factions?.Select(faction => new SystemFactionData {
                    Name = faction.Name,
                    State = faction.FactionState,
                    Government = faction.Government,
                    Influence = faction.Influence,
                    Allegiance = faction.Allegiance,
                    Happiness = JournalDisplayText.Format(faction.Happiness.ToString()),
                    Reputation = faction.MyReputation,
                    ActiveStates = faction.ActiveStates?.Select(state => state.State).ToArray() ?? Array.Empty<string>(),
                    RecoveringStates = faction.RecoveringStates?.Select(state => state.State).ToArray() ?? Array.Empty<string>()
                }).ToArray() ?? Array.Empty<SystemFactionData>()
            };
        }
    }
}
