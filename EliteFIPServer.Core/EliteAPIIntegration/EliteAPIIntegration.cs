using EliteAPI;
using EliteAPI.Events;
using EliteAPI.Events.Game;
using EliteAPI.Journals;
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
        private SystemExplorationData currentSystemExploration = new SystemExplorationData();
        private ExobiologyData currentExobiology = new ExobiologyData();
        private CommanderData currentCommander = new CommanderData();
        private CombatEarningsData currentCombatEarnings = new CombatEarningsData();
        private MiningData currentMining = new MiningData();
        private TradeData currentTrade = new TradeData();
        private CarrierData currentCarrier = new CarrierData();
        private OnFootData currentOnFoot = new OnFootData();
        private string currentCommanderName = "unavailable";

        // Working state behind currentSystemExploration and currentMissions.Active, guarded by trackingLock
        // because startup hydration can overlap live journal events.
        private readonly object trackingLock = new object();
        private readonly Dictionary<string, ExploredBody> exploredBodies = new Dictionary<string, ExploredBody>();
        private string explorationSystemId;
        private string explorationSystemName;
        private long explorationBodyCount;
        private long explorationNonBodyCount;
        private double explorationProgress;
        private bool explorationAllBodiesFound;
        private readonly Dictionary<string, MissionSummaryData> activeMissions = new Dictionary<string, MissionSummaryData>();
        // Set while replaying this system's scans at startup, so the last-scanned-body data keeps showing the current body.
        private bool hydratingExploration;
        // Working state behind currentExobiology, also guarded by trackingLock.
        private BioScan currentBioScan;
        private readonly List<BioScan> unsoldBioScans = new List<BioScan>();
        private long lastBioSaleValue;
        // Set while replaying organic scans at startup; the commander's position then is not the sample's.
        private bool hydratingExobiology;

        // Working state behind the commander, earnings, mining, trade, carrier and on-foot data, guarded by progressLock.
        private readonly object progressLock = new object();
        private readonly Dictionary<string, long> rankNumbers = new Dictionary<string, long>();
        private readonly Dictionary<string, long> rankProgress = new Dictionary<string, long>();
        private readonly Dictionary<string, EngineerData> engineers = new Dictionary<string, EngineerData>(StringComparer.OrdinalIgnoreCase);
        private long unredeemedBounties, bountyCount, unredeemedBonds, bondCount, lastRedeemed;
        // Set while replaying one kind of history, so a death replayed for it doesn't also clear the other kind.
        private bool hydratingCombatEarnings;
        private readonly Dictionary<string, long> refinedCommodities = new Dictionary<string, long>();
        private readonly Dictionary<string, (string Name, long Count)> cargoBySymbol = new Dictionary<string, (string, long)>();
        private Dictionary<string, (long SellPrice, long MeanPrice, long Demand)> marketPrices = new Dictionary<string, (long, long, long)>();
        private long sessionSales, sessionProfit, sessionPurchases;
        private string lastSale, marketStation, marketSystem;
        private DateTime marketUpdated;
        private IReadOnlyList<LockerItemData> shipLocker = Array.Empty<LockerItemData>();
        private IReadOnlyList<LockerItemData> backpack = Array.Empty<LockerItemData>();

        // Rank order as the panels show it.
        private static readonly string[] RankNames = { "Combat", "Trade", "Explore", "Soldier", "Exobiologist", "CQC", "Federation", "Empire" };

        private sealed class BioScan {
            public string SystemId;
            public string BodyId;
            public string BodyName;
            public string Genus;
            public string SpeciesSymbol;
            public string Species;
            public string Variant;
            public int SamplesTaken;
            public readonly List<BioSamplePositionData> Samples = new List<BioSamplePositionData>();
        }

        private sealed class ExploredBody {
            public string BodyId;
            public string BodyName;
            public string StarType;
            public string PlanetClass;
            public string TerraformState;
            public double DistanceFromArrivalLs;
            public double Mass;
            public bool IsLandable;
            public bool WasDiscovered;
            public bool WasMapped;
            public bool IsMapped;
            public bool MappedEfficiently;
            public IReadOnlyList<BodySignalData> Signals = Array.Empty<BodySignalData>();
            public bool IsScanned => !string.IsNullOrEmpty(StarType) || !string.IsNullOrEmpty(PlanetClass);
        }
        private string currentSystemName = "unavailable";
        private string currentShipName = "unavailable";
        private readonly string persistedRoutePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EliteFIPServer",
            "PanelRoute.json");

        public SystemExplorationData CurrentSystemExploration => currentSystemExploration;
        public ExobiologyData CurrentExobiology => currentExobiology;
        public CommanderData CurrentCommander => currentCommander;
        public CombatEarningsData CurrentCombatEarnings => currentCombatEarnings;
        public MiningData CurrentMining => currentMining;
        public TradeData CurrentTrade => currentTrade;
        public CarrierData CurrentCarrier => currentCarrier;
        public OnFootData CurrentOnFoot => currentOnFoot;
        public MaterialsData CurrentMaterials => currentMaterials;
        public MissionCollectionData CurrentMissions => currentMissions;
        public string CurrentCommanderName =>string.IsNullOrWhiteSpace(currentCommanderName) ? "unavailable" : currentCommanderName;
        public string CurrentSystemName {
            get {
                if (!string.IsNullOrWhiteSpace(currentLocation.SystemName)) { return currentLocation.SystemName; }
                return string.IsNullOrWhiteSpace(currentSystemName) ? "unavailable" : currentSystemName;
            }
        }
        public string CurrentShipName {
            get {
                if (!string.IsNullOrWhiteSpace(currentShipName)) { return currentShipName; }
                return string.IsNullOrWhiteSpace(currentLoadout.Ship) ? "unavailable" : currentLoadout.Ship;
            }
        }

        public EliteAPIIntegration(CoreServer coreServer) {
            CoreServer = coreServer;
            CurrentState = new ComponentState();
            currentNavRoute = LoadPersistedRoute();

            EliteAPI = new EliteDangerousApi();

            // Add events to watch list            
            EliteAPI.OnJson("Status", HandleStatusEvent);
            EliteAPI.On<ShipTargetedEvent>(HandleShipTargetedEvent);
            EliteAPI.On<CommanderEvent>(HandleCommanderEvent);
            EliteAPI.On<LoadGameEvent>(HandleLoadGameEvent);
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
            EliteAPI.On<MissionRedirectedEvent>(HandleMissionRedirectedEvent);
            EliteAPI.On<FssDiscoveryScanEvent>(HandleFssDiscoveryScanEvent);
            EliteAPI.On<FssAllBodiesFoundEvent>(HandleFssAllBodiesFoundEvent);
            EliteAPI.On<SaaScanCompleteEvent>(HandleSaaScanCompleteEvent);
            EliteAPI.On<SaaSignalsFoundEvent>(HandleSaaSignalsFoundEvent);
            EliteAPI.On<ScanOrganicEvent>(HandleScanOrganicEvent);
            EliteAPI.On<SellOrganicDataEvent>(HandleSellOrganicDataEvent);
            EliteAPI.On<DiedEvent>(HandleDiedEvent);
            EliteAPI.On<RankEvent>(HandleRankEvent);
            EliteAPI.On<ProgressEvent>(HandleProgressEvent);
            EliteAPI.OnJson("Promotion", HandlePromotionJson);
            EliteAPI.On<ReputationEvent>(HandleReputationEvent);
            EliteAPI.On<PowerplayEvent>(HandlePowerplayEvent);
            EliteAPI.OnJson("EngineerProgress", HandleEngineerProgressJson);
            EliteAPI.On<FactionKillBondEvent>(HandleFactionKillBondEvent);
            EliteAPI.On<RedeemVoucherEvent>(HandleRedeemVoucherEvent);
            EliteAPI.On<ProspectedAsteroidEvent>(HandleProspectedAsteroidEvent);
            EliteAPI.On<MiningRefinedEvent>(HandleMiningRefinedEvent);
            EliteAPI.On<MarketSellEvent>(HandleMarketSellEvent);
            EliteAPI.On<MarketBuyEvent>(HandleMarketBuyEvent);
            EliteAPI.OnJson("Market", HandleMarketJson);
            EliteAPI.On<CarrierStatsEvent>(HandleCarrierStatsEvent);
            EliteAPI.On<CarrierFinanceEvent>(HandleCarrierFinanceEvent);
            EliteAPI.On<CarrierJumpRequestEvent>(HandleCarrierJumpRequestEvent);
            EliteAPI.On<CarrierJumpCancelledEvent>(HandleCarrierJumpCancelledEvent);
            EliteAPI.On<SuitLoadoutEvent>(HandleSuitLoadoutEvent);
            EliteAPI.On<SwitchSuitLoadoutEvent>(HandleSwitchSuitLoadoutEvent);
            EliteAPI.OnJson("ShipLocker", HandleShipLockerJson);
            EliteAPI.OnJson("Backpack", HandleBackpackJson);
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

                // Commander identity comes from separate journal events, so hydrate them independently of location state.
                foreach (var identityEvent in new[] { "Commander", "LoadGame" }) {
                    var latestIdentityLine = journalLines
                        .Reverse()
                        .FirstOrDefault(line => string.Equals(GetEventName(line), identityEvent, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(latestIdentityLine)) {
                        InvokeHydratedJournalLine(latestIdentityLine);
                    }
                }

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

                // Missions accepted, finished or redirected since the Missions list was written at login.
                ReplayJournalEventsAfter(journalLines, new[] { "Missions" },
                    "MissionAccepted", "MissionCompleted", "MissionFailed", "MissionAbandoned", "MissionRedirected");

                // Everything already scanned in the current system.
                hydratingExploration = true;
                try {
                    ReplayJournalEventsAfter(journalLines, new[] { "FSDJump", "Location", "CarrierJump" },
                        "FSSDiscoveryScan", "FSSAllBodiesFound", "Scan", "SAAScanComplete", "SAASignalsFound");
                } finally {
                    hydratingExploration = false;
                }

                // This game session's ranks, reputation, engineers, suit, mining and trading, written after LoadGame.
                ReplayJournalEventsAfter(journalLines, new[] { "LoadGame" },
                    "Rank", "Progress", "Promotion", "Reputation", "Powerplay", "EngineerProgress", "SuitLoadout", "SwitchSuitLoadout",
                    "ProspectedAsteroid", "MiningRefined", "MarketSell", "MarketBuy");

                HydrateAcrossSessions(journalsDirectory);

                // Snapshot files Elite keeps current.
                foreach (var snapshot in new (string File, Action<(string, string)> Handler)[] {
                    ("Cargo.json", HandleCargoJson), ("Market.json", HandleMarketJson),
                    ("ShipLocker.json", HandleShipLockerJson), ("Backpack.json", HandleBackpackJson) }) {
                    var path = Path.Combine(journalsDirectory.FullName, snapshot.File);
                    if (File.Exists(path)) {
                        snapshot.Handler((Path.GetFileNameWithoutExtension(snapshot.File), ReadSharedText(path)));
                    }
                }
            } catch (Exception ex) {
                Log.Instance.Warn("Initial Elite API state hydration failed: {exception}", ex.ToString());
            }
        }

        // Replays the given events that follow the last of the marker events (or the whole file if there is none).
        private void ReplayJournalEventsAfter(string[] journalLines, string[] markerEvents, params string[] replayEvents) {
            var markers = new HashSet<string>(markerEvents, StringComparer.OrdinalIgnoreCase);
            var replay = new HashSet<string>(replayEvents, StringComparer.OrdinalIgnoreCase);
            int start = Array.FindLastIndex(journalLines, line => markers.Contains(GetEventName(line) ?? "")) + 1;
            for (int index = start; index < journalLines.Length; index++) {
                if (replay.Contains(GetEventName(journalLines[index]) ?? "")) {
                    InvokeHydratedJournalLine(journalLines[index]);
                }
            }
        }

        // State that outlives a game session: unsold organic data, unredeemed vouchers and the commander's carrier.
        // Each is replayed from the newest journals back to the one holding the event that last settled it.
        private void HydrateAcrossSessions(DirectoryInfo journalsDirectory) {
            var files = journalsDirectory.GetFiles("Journal.*.log").OrderByDescending(file => file.LastWriteTimeUtc).Take(MaxReplayJournals).ToArray();
            var cache = new Dictionary<int, string[]>();
            string[] Journal(int index) => cache.TryGetValue(index, out var lines) ? lines : cache[index] = ReadJournalLines(files[index].FullName);

            void ReplayBackTo(string[] stopEvents, params string[] replayEvents) {
                var stops = new HashSet<string>(stopEvents, StringComparer.OrdinalIgnoreCase);
                int oldest = files.Length - 1;
                for (int index = 0; index < files.Length; index++) {
                    if (Journal(index).Any(line => stopEvents.Any(line.Contains) && stops.Contains(GetEventName(line) ?? ""))) {
                        oldest = index;
                        break;
                    }
                }
                for (int index = oldest; index >= 0; index--) {
                    ReplayJournalEventsAfter(Journal(index), Array.Empty<string>(), replayEvents);
                }
            }

            hydratingExobiology = true;
            try {
                ReplayBackTo(new[] { "SellOrganicData", "Died" }, "ScanOrganic", "SellOrganicData", "Died");
            } finally {
                hydratingExobiology = false;
            }
            hydratingCombatEarnings = true;
            try {
                ReplayBackTo(new[] { "RedeemVoucher", "Died" }, "Bounty", "FactionKillBond", "RedeemVoucher", "Died");
            } finally {
                hydratingCombatEarnings = false;
            }
            ReplayBackTo(new[] { "CarrierStats" }, "CarrierStats", "CarrierFinance", "CarrierJumpRequest", "CarrierJumpCancelled");
        }

        private const int MaxReplayJournals = 100;

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
                currentCargo, currentMaterials, currentSystem, currentSystemExploration, currentExobiology, new (GameEventType, object)[] {
                    (GameEventType.Commander, currentCommander), (GameEventType.CombatEarnings, currentCombatEarnings),
                    (GameEventType.Mining, currentMining), (GameEventType.Trade, currentTrade),
                    (GameEventType.Carrier, currentCarrier), (GameEventType.OnFoot, currentOnFoot) });
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

        public void HandleCommanderEvent(CommanderEvent commanderData) {
            if (!IsRunning) { return; }

            currentCommanderName = string.IsNullOrWhiteSpace(commanderData.Name) ? "unavailable" : commanderData.Name;
        }

        public void HandleLoadGameEvent(LoadGameEvent loadGameData) {
            if (!IsRunning) { return; }

            if (!string.IsNullOrWhiteSpace(loadGameData.Commander)) {
                currentCommanderName = loadGameData.Commander;
            }
            if (!string.IsNullOrWhiteSpace(loadGameData.ShipName)) {
                currentShipName = loadGameData.ShipName;
            }

            // Mining and trading totals are per game session.
            UpdateMining(loadGameData.Timestamp, () => {
                lastProspect = null;
                firstRefined = default;
                refinedCommodities.Clear();
            });
            UpdateTrade(loadGameData.Timestamp, () => {
                sessionSales = sessionProfit = sessionPurchases = 0;
                lastSale = null;
            });
        }

        public void HandleLocationEvent(LocationEvent currentLocationData) {
            if (!IsRunning) { return; }

            Log.Instance.Info("Handling Location Event");
            currentSystemName = string.IsNullOrWhiteSpace(currentLocationData.StarSystem) ? currentSystemName : currentLocationData.StarSystem;

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
            UpdateSystemExploration(currentLocationData.Timestamp, currentLocationData.SystemAddress, currentLocationData.StarSystem, () => { });

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
            UpdateSystemExploration(fsdJumpdataData.Timestamp, fsdJumpdataData.SystemAddress, fsdJumpdataData.StarSystem, () => { });

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
                double[] previousPosition = null;
                foreach (NavRouteStopJson navRouteStop in currentNavRouteData.Route) {
                    NavigationData.NavRouteStop navStop = new NavigationData.NavRouteStop();
                    navStop.SystemId = navRouteStop.SystemAddress;
                    navStop.SystemName = navRouteStop.StarSystem;
                    navStop.Class = navRouteStop.StarClass;
                    navStop.StarPos = navRouteStop.StarPos ?? Array.Empty<double>();
                    navStop.JumpDistance = DistanceBetween(previousPosition, navStop.StarPos);
                    previousPosition = navStop.StarPos;
                    currentNavRoute.Stops.Add(navStop);
                }
                SavePersistedRoute();
                CoreServer.GameDataEvent(GameEventType.Navigation, currentNavRoute);                
            }

        }

        // Straight-line light years between two star positions; zero if either is unknown.
        internal static double DistanceBetween(double[] from, double[] to) {
            if (from == null || to == null || from.Length < 3 || to.Length < 3) { return 0; }
            double x = to[0] - from[0], y = to[1] - from[1], z = to[2] - from[2];
            return Math.Round(Math.Sqrt(x * x + y * y + z * z), 2);
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
            UpdateSystemExploration(dockedData.Timestamp, dockedData.SystemAddress, dockedData.StarSystem, () => { });
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

            // Belt clusters and rings are scanned too, but have neither a star type nor a planet class.
            if (!string.IsNullOrEmpty(scanData.StarType) || !string.IsNullOrEmpty(scanData.PlanetClass)) {
                UpdateSystemExploration(scanData.Timestamp, scanData.SystemAddress, scanData.StarSystem, () => {
                    ExploredBody body = GetExploredBody(scanData.BodyId, scanData.BodyName);
                    body.StarType = scanData.StarType;
                    body.PlanetClass = scanData.PlanetClass;
                    body.TerraformState = scanData.TerraformState;
                    body.DistanceFromArrivalLs = scanData.DistanceFromArrivalLs;
                    body.Mass = string.IsNullOrEmpty(scanData.StarType) ? scanData.MassEm : scanData.StellarMass;
                    body.IsLandable = scanData.IsLandable;
                    body.WasDiscovered = scanData.WasDiscovered;
                    body.WasMapped = scanData.WasMapped;
                });
            }
            if (hydratingExploration) { return; }

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

            if (!string.IsNullOrWhiteSpace(loadoutData.ShipName)) {
                currentShipName = loadoutData.ShipName;
            }

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

            UpdateActiveMissions(missionData.Timestamp, () => {
                activeMissions[missionData.MissionId] = new MissionSummaryData {
                    MissionId = missionData.MissionId,
                    Name = string.IsNullOrWhiteSpace(missionData.LocalisedName) ? JournalDisplayText.Format(missionData.Name) : missionData.LocalisedName,
                    IsPassengerMission = missionData.PassengerCount > 0,
                    ExpiresInSeconds = missionData.Expiry == default ? 0 : (long)Math.Max(0, (missionData.Expiry - missionData.Timestamp).TotalSeconds),
                    Expiry = missionData.Expiry,
                    Faction = missionData.Faction,
                    DestinationSystem = missionData.DestinationSystem,
                    DestinationStation = missionData.DestinationStation,
                    Reward = missionData.Reward,
                    Count = missionData.Count,
                    Commodity = missionData.Commodity,
                    Target = missionData.Target,
                    IsWing = missionData.IsWing
                };
            });
        }

        public void HandleMissionsEvent(MissionsEvent missionsData) {
            if (!IsRunning) { return; }
            lock (trackingLock) {
                // The Missions list only has names and expiry times, so keep the details of missions already known.
                var known = new Dictionary<string, MissionSummaryData>(activeMissions);
                activeMissions.Clear();
                foreach (var mission in missionsData.Active ?? Array.Empty<MissionsEvent.MissionInfo>()) {
                    var expiry = missionsData.Timestamp.AddSeconds(mission.Expires);
                    activeMissions[mission.MissionId] = known.TryGetValue(mission.MissionId, out var details)
                        ? CopyMission(details, expiry: expiry, expiresInSeconds: mission.Expires)
                        : new MissionSummaryData {
                            MissionId = mission.MissionId,
                            Name = JournalDisplayText.Format(mission.Name),
                            IsPassengerMission = mission.IsPassengerMission,
                            ExpiresInSeconds = mission.Expires,
                            Expiry = expiry
                        };
                }

                currentMissions = new MissionCollectionData {
                    LastUpdate = missionsData.Timestamp,
                    Active = SortedActiveMissions(),
                    Failed = MapMissionSummaries(missionsData.Failed),
                    Complete = MapMissionSummaries(missionsData.Complete)
                };
            }
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
            UpdateActiveMissions(missionData.Timestamp, () => activeMissions.Remove(missionData.MissionId));
        }

        public void HandleMissionFailedEvent(MissionFailedEvent missionData) {
            if (!IsRunning) { return; }
            CoreServer.GameDataEvent(GameEventType.MissionLifecycle, new MissionLifecycleData {
                LastUpdate = missionData.Timestamp, Operation = "Failed", MissionId = missionData.MissionId,
                Name = missionData.Name, LocalisedName = missionData.LocalisedName, Fine = missionData.Fine
            });
            UpdateActiveMissions(missionData.Timestamp, () => activeMissions.Remove(missionData.MissionId));
        }

        public void HandleMissionAbandonedEvent(MissionAbandonedEvent missionData) {
            if (!IsRunning) { return; }
            CoreServer.GameDataEvent(GameEventType.MissionLifecycle, new MissionLifecycleData {
                LastUpdate = missionData.Timestamp, Operation = "Abandoned", MissionId = missionData.MissionId,
                Name = missionData.Name, LocalisedName = missionData.LocalisedName
            });
            UpdateActiveMissions(missionData.Timestamp, () => activeMissions.Remove(missionData.MissionId));
        }

        public void HandleMissionRedirectedEvent(MissionRedirectedEvent missionData) {
            if (!IsRunning) { return; }
            UpdateActiveMissions(missionData.Timestamp, () => {
                if (activeMissions.TryGetValue(missionData.MissionId, out var mission)) {
                    activeMissions[missionData.MissionId] = CopyMission(mission,
                        destinationSystem: missionData.NewDestinationSystem,
                        destinationStation: missionData.NewDestinationStation);
                }
            });
        }

        private void UpdateActiveMissions(DateTime timestamp, Action change) {
            lock (trackingLock) {
                change();
                currentMissions = new MissionCollectionData {
                    LastUpdate = timestamp,
                    Active = SortedActiveMissions(),
                    Failed = currentMissions.Failed,
                    Complete = currentMissions.Complete
                };
            }
            CoreServer.GameDataEvent(GameEventType.MissionCollection, currentMissions);
        }

        private IReadOnlyList<MissionSummaryData> SortedActiveMissions() {
            return activeMissions.Values
                .OrderBy(mission => mission.Expiry == default ? DateTime.MaxValue : mission.Expiry)
                .ToArray();
        }

        private static MissionSummaryData CopyMission(MissionSummaryData source, DateTime? expiry = null, long? expiresInSeconds = null,
            string destinationSystem = null, string destinationStation = null) {
            return new MissionSummaryData {
                MissionId = source.MissionId,
                Name = source.Name,
                IsPassengerMission = source.IsPassengerMission,
                ExpiresInSeconds = expiresInSeconds ?? source.ExpiresInSeconds,
                Expiry = expiry ?? source.Expiry,
                Faction = source.Faction,
                DestinationSystem = destinationSystem ?? source.DestinationSystem,
                DestinationStation = destinationStation ?? source.DestinationStation,
                Reward = source.Reward,
                Count = source.Count,
                Commodity = source.Commodity,
                Target = source.Target,
                IsWing = source.IsWing
            };
        }

        public void HandleFssDiscoveryScanEvent(FssDiscoveryScanEvent scanData) {
            if (!IsRunning) { return; }
            UpdateSystemExploration(scanData.Timestamp, scanData.SystemAddress, scanData.SystemName, () => {
                explorationBodyCount = scanData.BodyCount;
                explorationNonBodyCount = scanData.NonBodyCount;
                explorationProgress = scanData.Progress;
            });
        }

        public void HandleFssAllBodiesFoundEvent(FssAllBodiesFoundEvent scanData) {
            if (!IsRunning) { return; }
            UpdateSystemExploration(scanData.Timestamp, scanData.SystemAddress, scanData.SystemName, () => {
                explorationAllBodiesFound = true;
                explorationBodyCount = Math.Max(explorationBodyCount, scanData.Count);
                explorationProgress = 1;
            });
        }

        public void HandleSaaScanCompleteEvent(SaaScanCompleteEvent scanData) {
            if (!IsRunning) { return; }
            UpdateSystemExploration(scanData.Timestamp, scanData.SystemAddress, null, () => {
                ExploredBody body = GetExploredBody(scanData.BodyId, scanData.BodyName);
                body.IsMapped = true;
                body.MappedEfficiently = scanData.EfficiencyTarget > 0 && scanData.ProbesUsed <= scanData.EfficiencyTarget;
            });
        }

        public void HandleSaaSignalsFoundEvent(SaaSignalsFoundEvent signalData) {
            if (!IsRunning) { return; }
            UpdateSystemExploration(signalData.Timestamp, signalData.SystemAddress, null, () => {
                ExploredBody body = GetExploredBody(signalData.BodyId, signalData.BodyName);
                body.Signals = signalData.Signals?.Select(signal => new BodySignalData {
                    Type = FormatSignalType(signal.Type.ToString(), signal.Type.Symbol),
                    Count = signal.Count
                }).ToArray() ?? Array.Empty<BodySignalData>();
            });
        }

        // ---- Commander: ranks, reputation, Powerplay and engineers ----

        private readonly Dictionary<string, double> superpowerReputation = new Dictionary<string, double>();
        private string pledgedPower;
        private long powerplayRank, powerplayMerits, timePledged;

        public void HandleRankEvent(RankEvent rankData) {
            if (!IsRunning) { return; }
            UpdateCommander(rankData.Timestamp, () => SetRanks(rankNumbers, rankData.Combat, rankData.Trade, rankData.Explore,
                rankData.Soldier, rankData.Exobiologist, rankData.Cqc, rankData.Federation, rankData.Empire));
        }

        public void HandleProgressEvent(ProgressEvent progressData) {
            if (!IsRunning) { return; }
            UpdateCommander(progressData.Timestamp, () => SetRanks(rankProgress, progressData.Combat, progressData.Trade, progressData.Explore,
                progressData.Soldier, progressData.Exobiologist, progressData.Cqc, progressData.Federation, progressData.Empire));
        }

        // Values in RankNames order.
        private static void SetRanks(Dictionary<string, long> target, params long[] values) {
            for (int index = 0; index < RankNames.Length; index++) {
                target[RankNames[index]] = values[index];
            }
        }

        // Promotion carries only the ranks that changed, e.g. { "Combat": 5 }.
        public void HandlePromotionJson((string eventName, string json) promotion) {
            if (!IsRunning) { return; }
            try {
                var json = JObject.Parse(promotion.json);
                UpdateCommander(json["timestamp"]?.Value<DateTime>() ?? DateTime.UtcNow, () => {
                    foreach (var name in RankNames) {
                        var value = json.Properties().FirstOrDefault(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))?.Value;
                        if (value?.Type == JTokenType.Integer) {
                            rankNumbers[name] = value.Value<long>();
                            rankProgress[name] = 0;
                        }
                    }
                });
            } catch (JsonException) { }
        }

        public void HandleReputationEvent(ReputationEvent reputationData) {
            if (!IsRunning) { return; }
            UpdateCommander(reputationData.Timestamp, () => {
                superpowerReputation["Federation"] = reputationData.Federation;
                superpowerReputation["Empire"] = reputationData.Empire;
                superpowerReputation["Alliance"] = reputationData.Alliance;
                superpowerReputation["Independent"] = reputationData.Independent;
            });
        }

        public void HandlePowerplayEvent(PowerplayEvent powerplayData) {
            if (!IsRunning) { return; }
            UpdateCommander(powerplayData.Timestamp, () => {
                pledgedPower = powerplayData.Power;
                powerplayRank = powerplayData.Rank;
                powerplayMerits = powerplayData.Merits;
                timePledged = powerplayData.TimePledged;
            });
        }

        // At login EngineerProgress lists every engineer; later ones describe a single engineer at the top level.
        public void HandleEngineerProgressJson((string eventName, string json) progress) {
            if (!IsRunning) { return; }
            try {
                var json = JObject.Parse(progress.json);
                static EngineerData Engineer(JToken item) => new EngineerData {
                    Name = item["Engineer"]?.Value<string>(),
                    Progress = item["Progress"]?.Value<string>(),
                    Rank = item["Rank"]?.Value<long>() ?? 0,
                    RankProgress = item["RankProgress"]?.Value<long>() ?? 0
                };
                UpdateCommander(json["timestamp"]?.Value<DateTime>() ?? DateTime.UtcNow, () => {
                    var updates = json["Engineers"] is JArray list ? list.Select(Engineer) : new[] { Engineer(json) };
                    foreach (var engineer in updates.Where(engineer => !string.IsNullOrWhiteSpace(engineer.Name))) {
                        engineers[engineer.Name] = engineer;
                    }
                });
            } catch (JsonException) { }
        }

        private void UpdateCommander(DateTime timestamp, Action change) {
            CommanderData published;
            lock (progressLock) {
                change();
                published = new CommanderData {
                    LastUpdate = timestamp,
                    Ranks = RankNames.Where(rankNumbers.ContainsKey).Select(name => new CommanderRankData {
                        Name = name, Rank = rankNumbers[name], Progress = rankProgress.TryGetValue(name, out long percent) ? percent : 0
                    }).ToArray(),
                    Reputation = superpowerReputation.Select(item => new SuperpowerReputationData { Superpower = item.Key, Reputation = item.Value }).ToArray(),
                    Power = pledgedPower,
                    PowerplayRank = powerplayRank,
                    Merits = powerplayMerits,
                    TimePledgedSeconds = timePledged,
                    Engineers = engineers.Values.OrderBy(engineer => engineer.Name, StringComparer.OrdinalIgnoreCase).ToArray()
                };
                currentCommander = published;
            }
            CoreServer.GameDataEvent(GameEventType.Commander, published);
        }

        // ---- Combat earnings: bounty vouchers and combat bonds not yet handed in ----

        public void HandleFactionKillBondEvent(FactionKillBondEvent bondData) {
            if (!IsRunning) { return; }
            UpdateCombatEarnings(bondData.Timestamp, () => {
                unredeemedBonds += bondData.Reward;
                bondCount++;
            });
        }

        // Redeeming at one faction's broker can leave others' vouchers, so the amount is subtracted rather than
        // the total cleared. Amounts after a broker's cut are slightly lower than earned, so totals round down to zero.
        public void HandleRedeemVoucherEvent(RedeemVoucherEvent voucherData) {
            if (!IsRunning) { return; }
            UpdateCombatEarnings(voucherData.Timestamp, () => {
                double share = voucherData.BrokerPercentage > 0 ? 1 - voucherData.BrokerPercentage / 100 : 1;
                long settled = (long)Math.Round(voucherData.Amount / share);
                if (string.Equals(voucherData.Type, "bounty", StringComparison.OrdinalIgnoreCase)) {
                    unredeemedBounties = Math.Max(0, unredeemedBounties - settled);
                    if (unredeemedBounties == 0) { bountyCount = 0; }
                } else if (string.Equals(voucherData.Type, "CombatBond", StringComparison.OrdinalIgnoreCase)) {
                    unredeemedBonds = Math.Max(0, unredeemedBonds - settled);
                    if (unredeemedBonds == 0) { bondCount = 0; }
                } else {
                    return;
                }
                lastRedeemed = voucherData.Amount;
            });
        }

        private void UpdateCombatEarnings(DateTime timestamp, Action change) {
            CombatEarningsData published;
            lock (progressLock) {
                change();
                published = new CombatEarningsData {
                    LastUpdate = timestamp,
                    UnredeemedBounties = unredeemedBounties,
                    BountyCount = bountyCount,
                    UnredeemedBonds = unredeemedBonds,
                    BondCount = bondCount,
                    LastRedeemed = lastRedeemed
                };
                currentCombatEarnings = published;
            }
            CoreServer.GameDataEvent(GameEventType.CombatEarnings, published);
        }

        // ---- Mining ----

        private ProspectedAsteroidData lastProspect;
        private DateTime firstRefined;

        public void HandleProspectedAsteroidEvent(ProspectedAsteroidEvent prospectData) {
            if (!IsRunning) { return; }
            string content = prospectData.Content.Symbol ?? "";
            UpdateMining(prospectData.Timestamp, () => lastProspect = new ProspectedAsteroidData {
                Timestamp = prospectData.Timestamp,
                // "$AsteroidMaterialContent_High;" becomes "High".
                Content = content.Contains("High", StringComparison.OrdinalIgnoreCase) ? "High"
                    : content.Contains("Medium", StringComparison.OrdinalIgnoreCase) ? "Medium"
                    : content.Contains("Low", StringComparison.OrdinalIgnoreCase) ? "Low" : LocalisedText(prospectData.Content),
                Motherlode = string.IsNullOrEmpty(prospectData.Motherlode.Symbol) ? null : LocalisedText(prospectData.Motherlode),
                Remaining = prospectData.Remaining,
                Materials = prospectData.Materials?
                    .Select(material => new ProspectedMaterialData { Name = LocalisedText(material.Name), Percent = Math.Round(material.Proportion, 2) })
                    .OrderByDescending(material => material.Percent).ToArray() ?? Array.Empty<ProspectedMaterialData>()
            });
        }

        public void HandleMiningRefinedEvent(MiningRefinedEvent refinedData) {
            if (!IsRunning) { return; }
            string name = LocalisedText(refinedData.Type);
            UpdateMining(refinedData.Timestamp, () => {
                if (firstRefined == default) { firstRefined = refinedData.Timestamp; }
                refinedCommodities[name] = (refinedCommodities.TryGetValue(name, out long count) ? count : 0) + 1;
            });
        }

        private void UpdateMining(DateTime timestamp, Action change) {
            MiningData published;
            lock (progressLock) {
                change();
                published = new MiningData {
                    LastUpdate = timestamp,
                    LastProspect = lastProspect,
                    FirstRefined = firstRefined,
                    TotalRefined = refinedCommodities.Values.Sum(),
                    Refined = refinedCommodities.OrderByDescending(item => item.Value)
                        .Select(item => new RefinedCommodityData { Name = item.Key, Count = item.Value }).ToArray()
                };
                currentMining = published;
            }
            CoreServer.GameDataEvent(GameEventType.Mining, published);
        }

        // ---- Trade: session totals and local prices for the cargo held ----

        public void HandleMarketSellEvent(MarketSellEvent saleData) {
            if (!IsRunning) { return; }
            UpdateTrade(saleData.Timestamp, () => {
                sessionSales += saleData.TotalSale;
                sessionProfit += saleData.TotalSale - saleData.AvgPricePaid * saleData.Count;
                lastSale = $"{saleData.Count} {LocalisedText(saleData.Type)} for {saleData.TotalSale:N0} CR";
            });
        }

        public void HandleMarketBuyEvent(MarketBuyEvent purchaseData) {
            if (!IsRunning) { return; }
            UpdateTrade(purchaseData.Timestamp, () => sessionPurchases += purchaseData.TotalCost);
        }

        // Market.json holds the last market opened; the journal's Market line has no items and is skipped.
        public void HandleMarketJson((string eventName, string json) market) {
            if (!IsRunning) { return; }
            try {
                var json = JObject.Parse(market.json);
                if (json["Items"] is not JArray items) { return; }
                UpdateTrade(json["timestamp"]?.Value<DateTime>() ?? DateTime.UtcNow, () => {
                    marketStation = json["StationName"]?.Value<string>();
                    marketSystem = json["StarSystem"]?.Value<string>();
                    marketUpdated = json["timestamp"]?.Value<DateTime>() ?? DateTime.UtcNow;
                    marketPrices = items
                        .GroupBy(item => CommodityKey(item["Name"]?.Value<string>()))
                        .Where(group => group.Key.Length > 0)
                        .ToDictionary(group => group.Key, group => (
                            group.First()["SellPrice"]?.Value<long>() ?? 0,
                            group.First()["MeanPrice"]?.Value<long>() ?? 0,
                            group.First()["Demand"]?.Value<long>() ?? 0));
                });
            } catch (JsonException) { }
        }

        // "$lowtemperaturediamond_name;" (Market.json) and "lowtemperaturediamond" (Cargo.json) both become "lowtemperaturediamond".
        internal static string CommodityKey(string symbol) {
            string key = (symbol ?? "").Trim('$', ';').ToLowerInvariant();
            return key.EndsWith("_name", StringComparison.Ordinal) ? key[..^5] : key;
        }

        private void UpdateTrade(DateTime timestamp, Action change) {
            TradeData published;
            lock (progressLock) {
                change();
                published = new TradeData {
                    LastUpdate = timestamp,
                    SessionSales = sessionSales,
                    SessionProfit = sessionProfit,
                    SessionPurchases = sessionPurchases,
                    LastSale = lastSale,
                    MarketStation = marketStation,
                    MarketSystem = marketSystem,
                    MarketUpdated = marketUpdated,
                    CargoPrices = cargoBySymbol
                        .Where(cargo => marketPrices.ContainsKey(cargo.Key))
                        .Select(cargo => new CargoPriceData {
                            Name = cargo.Value.Name,
                            Count = cargo.Value.Count,
                            SellPrice = marketPrices[cargo.Key].SellPrice,
                            MeanPrice = marketPrices[cargo.Key].MeanPrice,
                            Demand = marketPrices[cargo.Key].Demand
                        })
                        .OrderByDescending(price => price.SellPrice * price.Count).ToArray()
                };
                currentTrade = published;
            }
            CoreServer.GameDataEvent(GameEventType.Trade, published);
        }

        // ---- The commander's fleet carrier ----

        private CarrierData carrierState = new CarrierData();

        public void HandleCarrierStatsEvent(CarrierStatsEvent stats) {
            if (!IsRunning) { return; }
            UpdateCarrier(stats.Timestamp, carrier => new CarrierData {
                Name = stats.Name,
                Callsign = stats.Callsign,
                FuelLevel = stats.FuelLevel,
                Balance = stats.Finance.CarrierBalance,
                ReserveBalance = stats.Finance.ReserveBalance,
                AvailableBalance = stats.Finance.AvailableBalance,
                FreeSpace = stats.SpaceUsage.FreeSpace,
                TotalCapacity = stats.SpaceUsage.TotalCapacity,
                DockingAccess = stats.DockingAccess,
                IsPendingDecommission = stats.IsPendingDecommission,
                JumpDestinationSystem = carrier.JumpDestinationSystem,
                JumpDestinationBody = carrier.JumpDestinationBody,
                JumpDeparture = carrier.JumpDeparture
            });
        }

        public void HandleCarrierFinanceEvent(CarrierFinanceEvent finance) {
            if (!IsRunning) { return; }
            UpdateCarrier(finance.Timestamp, carrier => CopyCarrier(carrier, balance: finance.Balance,
                reserve: finance.ReserveBalance, available: finance.AvailableBalance));
        }

        public void HandleCarrierJumpRequestEvent(CarrierJumpRequestEvent request) {
            if (!IsRunning) { return; }
            DateTime? departure = DateTime.TryParse(request.DepartureTime, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var parsed) ? parsed : null;
            UpdateCarrier(request.Timestamp, carrier => CopyCarrier(carrier, jumpSystem: request.SystemName ?? "",
                jumpBody: request.Body ?? "", departure: departure, setJump: true));
        }

        public void HandleCarrierJumpCancelledEvent(CarrierJumpCancelledEvent cancelled) {
            if (!IsRunning) { return; }
            UpdateCarrier(cancelled.Timestamp, carrier => CopyCarrier(carrier, jumpSystem: null, jumpBody: null, departure: null, setJump: true));
        }

        private static CarrierData CopyCarrier(CarrierData carrier, long? balance = null, long? reserve = null, long? available = null,
            string jumpSystem = null, string jumpBody = null, DateTime? departure = null, bool setJump = false) => new CarrierData {
            Name = carrier.Name,
            Callsign = carrier.Callsign,
            FuelLevel = carrier.FuelLevel,
            Balance = balance ?? carrier.Balance,
            ReserveBalance = reserve ?? carrier.ReserveBalance,
            AvailableBalance = available ?? carrier.AvailableBalance,
            FreeSpace = carrier.FreeSpace,
            TotalCapacity = carrier.TotalCapacity,
            DockingAccess = carrier.DockingAccess,
            IsPendingDecommission = carrier.IsPendingDecommission,
            JumpDestinationSystem = setJump ? (string.IsNullOrEmpty(jumpSystem) ? null : jumpSystem) : carrier.JumpDestinationSystem,
            JumpDestinationBody = setJump ? (string.IsNullOrEmpty(jumpBody) ? null : jumpBody) : carrier.JumpDestinationBody,
            JumpDeparture = setJump ? departure : carrier.JumpDeparture
        };

        private void UpdateCarrier(DateTime timestamp, Func<CarrierData, CarrierData> change) {
            CarrierData published;
            lock (progressLock) {
                var next = change(carrierState);
                carrierState = next;
                published = new CarrierData {
                    LastUpdate = timestamp,
                    Name = next.Name, Callsign = next.Callsign, FuelLevel = next.FuelLevel, Balance = next.Balance,
                    ReserveBalance = next.ReserveBalance, AvailableBalance = next.AvailableBalance, FreeSpace = next.FreeSpace,
                    TotalCapacity = next.TotalCapacity, DockingAccess = next.DockingAccess, IsPendingDecommission = next.IsPendingDecommission,
                    JumpDestinationSystem = next.JumpDestinationSystem, JumpDestinationBody = next.JumpDestinationBody, JumpDeparture = next.JumpDeparture
                };
                currentCarrier = published;
            }
            CoreServer.GameDataEvent(GameEventType.Carrier, published);
        }

        // ---- On foot: suit loadout, ship locker and backpack ----

        private string suitName, suitLoadoutName;
        private IReadOnlyList<string> suitWeapons = Array.Empty<string>();

        public void HandleSuitLoadoutEvent(SuitLoadoutEvent loadout) =>
            SetSuitLoadout(loadout.Timestamp, loadout.SuitName, loadout.LoadoutName, loadout.Modules);

        public void HandleSwitchSuitLoadoutEvent(SwitchSuitLoadoutEvent loadout) =>
            SetSuitLoadout(loadout.Timestamp, loadout.SuitName, loadout.LoadoutName, loadout.Modules);

        private void SetSuitLoadout(DateTime timestamp, EliteAPI.Events.LocalisedField suit, string loadoutName,
            IReadOnlyCollection<SuitLoadoutEvent.LoadoutModuleInfo> modules) {
            if (!IsRunning) { return; }
            UpdateOnFoot(timestamp, () => {
                suitName = LocalisedText(suit);
                suitLoadoutName = loadoutName;
                suitWeapons = modules?.Select(module => LocalisedText(module.ModuleName)).ToArray() ?? Array.Empty<string>();
            });
        }

        // ShipLocker.json and Backpack.json are snapshots; journal lines of the same name without contents are skipped.
        public void HandleShipLockerJson((string eventName, string json) locker) {
            if (!IsRunning) { return; }
            var items = ParseLocker(locker.json, out DateTime timestamp);
            if (items != null) { UpdateOnFoot(timestamp, () => shipLocker = items); }
        }

        public void HandleBackpackJson((string eventName, string json) pack) {
            if (!IsRunning) { return; }
            var items = ParseLocker(pack.json, out DateTime timestamp);
            if (items != null) { UpdateOnFoot(timestamp, () => backpack = items); }
        }

        private static IReadOnlyList<LockerItemData> ParseLocker(string text, out DateTime timestamp) {
            timestamp = DateTime.UtcNow;
            try {
                var json = JObject.Parse(text);
                timestamp = json["timestamp"]?.Value<DateTime>() ?? timestamp;
                var categories = new[] { "Items", "Components", "Consumables", "Data" };
                if (!categories.Any(category => json[category] is JArray)) { return null; }
                return categories
                    .SelectMany(category => (json[category] as JArray ?? new JArray()).Select(item => new LockerItemData {
                        Name = item["Name_Localised"]?.Value<string>() ?? JournalDisplayText.Format(item["Name"]?.Value<string>()),
                        Category = category,
                        Count = item["Count"]?.Value<long>() ?? 0
                    }))
                    .GroupBy(item => (item.Category, item.Name))
                    .Select(group => new LockerItemData { Name = group.Key.Name, Category = group.Key.Category, Count = group.Sum(item => item.Count) })
                    .OrderBy(item => item.Category).ThenByDescending(item => item.Count).ToArray();
            } catch (JsonException) {
                return null;
            }
        }

        private void UpdateOnFoot(DateTime timestamp, Action change) {
            OnFootData published;
            lock (progressLock) {
                change();
                published = new OnFootData {
                    LastUpdate = timestamp,
                    SuitName = suitName,
                    LoadoutName = suitLoadoutName,
                    Weapons = suitWeapons,
                    ShipLocker = shipLocker,
                    Backpack = backpack
                };
                currentOnFoot = published;
            }
            CoreServer.GameDataEvent(GameEventType.OnFoot, published);
        }

        // ScanOrganic arrives as Log (first sample), Sample (second and third) and Analyse (straight after the third).
        // Starting a new species abandons the one in progress, as the game does.
        public void HandleScanOrganicEvent(ScanOrganicEvent scanData) {
            if (!IsRunning) { return; }
            UpdateExobiology(scanData.Timestamp, () => {
                string bodyId = scanData.Body.ToString();
                bool sameSpecies = currentBioScan != null
                    && string.Equals(currentBioScan.SpeciesSymbol, scanData.Species.Symbol, StringComparison.Ordinal)
                    && string.Equals(currentBioScan.BodyId, bodyId, StringComparison.Ordinal)
                    && string.Equals(currentBioScan.SystemId, scanData.SystemAddress, StringComparison.Ordinal);

                if (string.Equals(scanData.ScanType, "Analyse", StringComparison.OrdinalIgnoreCase)) {
                    BioScan analysed = sameSpecies ? currentBioScan : NewBioScan(scanData, bodyId);
                    analysed.SamplesTaken = 3;
                    unsoldBioScans.Add(analysed);
                    currentBioScan = null;
                    return;
                }

                if (string.Equals(scanData.ScanType, "Log", StringComparison.OrdinalIgnoreCase) || !sameSpecies) {
                    currentBioScan = NewBioScan(scanData, bodyId);
                    // A Sample without its Log (logged before the journal being read) is at least the second.
                    currentBioScan.SamplesTaken = string.Equals(scanData.ScanType, "Log", StringComparison.OrdinalIgnoreCase) ? 1 : 2;
                } else {
                    currentBioScan.SamplesTaken = Math.Min(3, currentBioScan.SamplesTaken + 1);
                }
                if (!hydratingExobiology && currentStatus.HasLatLong && currentStatus.PlanetRadius > 0) {
                    currentBioScan.Samples.Add(new BioSamplePositionData {
                        Latitude = currentStatus.Latitude,
                        Longitude = currentStatus.Longitude,
                        PlanetRadius = currentStatus.PlanetRadius
                    });
                }
            });
        }

        public void HandleSellOrganicDataEvent(SellOrganicDataEvent saleData) {
            if (!IsRunning) { return; }
            UpdateExobiology(saleData.Timestamp, () => {
                lastBioSaleValue = 0;
                foreach (var sold in saleData.BioData ?? Array.Empty<SellOrganicDataEvent.BioDataInfo>()) {
                    lastBioSaleValue += sold.Value + sold.Bonus;
                    int index = unsoldBioScans.FindIndex(scan => string.Equals(scan.SpeciesSymbol, sold.Species.Symbol, StringComparison.Ordinal));
                    if (index >= 0) { unsoldBioScans.RemoveAt(index); }
                }
            });
        }

        // Unsold organic data is lost on death.
        // Unsold organic data, bounty vouchers and combat bonds are all lost on death.
        public void HandleDiedEvent(DiedEvent diedData) {
            if (!IsRunning) { return; }
            if (!hydratingCombatEarnings) {
                UpdateExobiology(diedData.Timestamp, () => {
                    currentBioScan = null;
                    unsoldBioScans.Clear();
                });
            }
            if (!hydratingExobiology) {
                UpdateCombatEarnings(diedData.Timestamp, () => {
                    unredeemedBounties = bountyCount = unredeemedBonds = bondCount = 0;
                });
            }
        }

        private BioScan NewBioScan(ScanOrganicEvent scanData, string bodyId) {
            string bodyName = null;
            if (string.Equals(scanData.SystemAddress, explorationSystemId, StringComparison.Ordinal)
                && exploredBodies.TryGetValue(bodyId, out var body)) {
                bodyName = body.BodyName;
            }
            return new BioScan {
                SystemId = scanData.SystemAddress,
                BodyId = bodyId,
                BodyName = bodyName ?? (string.IsNullOrWhiteSpace(currentStatus.BodyName) ? currentLocation.BodyName : currentStatus.BodyName),
                Genus = LocalisedText(scanData.Genus),
                SpeciesSymbol = scanData.Species.Symbol,
                Species = LocalisedText(scanData.Species),
                Variant = LocalisedText(scanData.Variant)
            };
        }

        // The localised name, or a readable form of the symbol ("$Codex_Ent_Stratum_07_Name;") when there is none.
        private static string LocalisedText(EliteAPI.Events.LocalisedField field) {
            string local = field.ToString();
            if (!string.IsNullOrWhiteSpace(local) && !local.StartsWith("$", StringComparison.Ordinal)) { return local; }
            string symbol = (field.Symbol ?? "").Trim('$', ';');
            if (symbol.StartsWith("Codex_Ent_", StringComparison.OrdinalIgnoreCase)) { symbol = symbol[10..]; }
            if (symbol.EndsWith("_Name", StringComparison.OrdinalIgnoreCase)) { symbol = symbol[..^5]; }
            return JournalDisplayText.Format(symbol);
        }

        private void UpdateExobiology(DateTime timestamp, Action change) {
            ExobiologyData published;
            lock (trackingLock) {
                change();
                var unsold = unsoldBioScans.Select(CreateBioScanData).ToArray();
                published = new ExobiologyData {
                    LastUpdate = timestamp,
                    Current = currentBioScan == null ? null : CreateBioScanData(currentBioScan),
                    Unsold = unsold,
                    UnsoldEstimatedValue = unsold.Sum(scan => scan.EstimatedValue),
                    LastSaleValue = lastBioSaleValue
                };
                currentExobiology = published;
            }
            CoreServer.GameDataEvent(GameEventType.Exobiology, published);
        }

        private static BioScanData CreateBioScanData(BioScan scan) => new BioScanData {
            SystemId = scan.SystemId,
            BodyId = scan.BodyId,
            BodyName = scan.BodyName,
            Genus = scan.Genus,
            Species = scan.Species,
            Variant = scan.Variant,
            SamplesTaken = scan.SamplesTaken,
            SampleDistance = ExobiologyValues.SampleDistance(scan.Genus),
            EstimatedValue = ExobiologyValues.EstimateSpecies(scan.Species),
            Samples = scan.Samples.ToArray()
        };

        private static string FormatSignalType(string localised, string symbol) {
            if (!string.IsNullOrWhiteSpace(localised) && !localised.StartsWith("$", StringComparison.Ordinal)) {
                return localised;
            }
            // "$SAA_SignalType_Biological;" becomes "Biological".
            string name = (symbol ?? localised ?? "").Trim('$', ';');
            int lastUnderscore = name.LastIndexOf('_');
            return JournalDisplayText.Format(lastUnderscore >= 0 ? name[(lastUnderscore + 1)..] : name);
        }

        // Applies a change to the current system's exploration state, starting afresh if the event belongs to
        // another system, and publishes the result.
        private void UpdateSystemExploration(DateTime timestamp, string systemId, string systemName, Action change) {
            SystemExplorationData published;
            lock (trackingLock) {
                if (!string.IsNullOrEmpty(systemId) && !string.Equals(systemId, explorationSystemId, StringComparison.Ordinal)) {
                    explorationSystemId = systemId;
                    explorationSystemName = null;
                    explorationBodyCount = 0;
                    explorationNonBodyCount = 0;
                    explorationProgress = 0;
                    explorationAllBodiesFound = false;
                    exploredBodies.Clear();
                }
                if (!string.IsNullOrWhiteSpace(systemName)) {
                    explorationSystemName = systemName;
                }

                change();

                var bodies = exploredBodies.Values
                    .Where(body => body.IsScanned || body.IsMapped || body.Signals.Count > 0)
                    .OrderBy(body => body.DistanceFromArrivalLs)
                    .ThenBy(body => body.BodyName, StringComparer.OrdinalIgnoreCase)
                    .Select(CreateExploredBodyData)
                    .ToArray();
                published = new SystemExplorationData {
                    LastUpdate = timestamp,
                    SystemId = explorationSystemId,
                    SystemName = explorationSystemName,
                    BodyCount = explorationBodyCount,
                    NonBodyCount = explorationNonBodyCount,
                    DiscoveryProgress = explorationProgress,
                    AllBodiesFound = explorationAllBodiesFound,
                    EstimatedValue = bodies.Sum(body => body.EstimatedValue),
                    Bodies = bodies
                };
                currentSystemExploration = published;
            }
            CoreServer.GameDataEvent(GameEventType.SystemExploration, published);
        }

        private ExploredBody GetExploredBody(string bodyId, string bodyName) {
            string key = string.IsNullOrEmpty(bodyId) ? bodyName ?? "" : bodyId;
            if (!exploredBodies.TryGetValue(key, out var body)) {
                body = new ExploredBody { BodyId = bodyId, BodyName = bodyName };
                exploredBodies[key] = body;
            }
            if (!string.IsNullOrWhiteSpace(bodyName)) {
                body.BodyName = bodyName;
            }
            return body;
        }

        private static ExploredBodyData CreateExploredBodyData(ExploredBody body) {
            bool isStar = !string.IsNullOrEmpty(body.StarType);
            bool firstDiscovery = body.IsScanned && !body.WasDiscovered;
            long value = !body.IsScanned ? 0
                : isStar ? ExplorationValues.EstimateStar(body.StarType, body.Mass, firstDiscovery)
                : ExplorationValues.EstimatePlanet(body.PlanetClass, body.TerraformState, body.Mass, firstDiscovery,
                    body.IsMapped, !body.WasMapped, body.MappedEfficiently);
            long mappedValue = !body.IsScanned || isStar ? 0
                : ExplorationValues.EstimatePlanet(body.PlanetClass, body.TerraformState, body.Mass, firstDiscovery,
                    true, !body.WasMapped, body.IsMapped ? body.MappedEfficiently : true);

            return new ExploredBodyData {
                BodyId = body.BodyId,
                BodyName = body.BodyName,
                BodyType = isStar ? "Star" : body.IsScanned ? "Planet" : "",
                StarType = body.StarType,
                PlanetClass = body.PlanetClass,
                TerraformState = body.TerraformState,
                DistanceFromArrivalLs = body.DistanceFromArrivalLs,
                IsLandable = body.IsLandable,
                WasDiscovered = body.WasDiscovered,
                WasMapped = body.WasMapped,
                IsMapped = body.IsMapped,
                MappedEfficiently = body.MappedEfficiently,
                EstimatedValue = value,
                EstimatedMappedValue = mappedValue,
                Signals = body.Signals
            };
        }

        public void HandleMaterialsEvent(MaterialsEvent materialsData) {
            if (!IsRunning) { return; }
            currentMaterials = new MaterialsData {
                LastUpdate = materialsData.Timestamp,
                Raw = materialsData.Raw?.Select(item => NewMaterial(JournalDisplayText.Format(item.Name), item.Name, item.Count, "Raw")).ToArray() ?? Array.Empty<MaterialItemData>(),
                Manufactured = materialsData.Manufactured?.Select(item => NewMaterial(JournalDisplayText.Format(item.Name.ToString()), item.Name.Symbol, item.Count, "Manufactured")).ToArray() ?? Array.Empty<MaterialItemData>(),
                Encoded = materialsData.Encoded?.Select(item => NewMaterial(JournalDisplayText.Format(item.Name.ToString()), item.Name.Symbol, item.Count, "Encoded")).ToArray() ?? Array.Empty<MaterialItemData>()
            };
            CoreServer.GameDataEvent(GameEventType.Materials, currentMaterials);
        }

        public void HandleMaterialCollectedEvent(MaterialCollectedEvent data) {
            UpdateMaterial(data.Timestamp, data.Category, JournalDisplayText.Format(data.Name.ToString()), data.Name.Symbol, data.Count);
        }

        public void HandleMaterialDiscardedEvent(MaterialDiscardedEvent data) {
            UpdateMaterial(data.Timestamp, data.Category, JournalDisplayText.Format(data.Name), data.Name, -data.Count);
        }

        public void HandleMaterialTradeEvent(MaterialTradeEvent data) {
            UpdateMaterial(data.Timestamp, data.Paid.Category, JournalDisplayText.Format(data.Paid.Material.ToString()), data.Paid.Material.Symbol, -data.Paid.Quantity, false);
            UpdateMaterial(data.Timestamp, data.Received.Category, JournalDisplayText.Format(data.Received.Material.ToString()), data.Received.Material.Symbol, data.Received.Quantity);
        }

        private static MaterialItemData NewMaterial(string name, string symbol, long count, string category) {
            int grade = MaterialGrades.Grade(symbol);
            return new MaterialItemData { Name = name, Count = count, Category = category, Grade = grade, Maximum = MaterialGrades.Maximum(grade) };
        }

        private void UpdateMaterial(DateTime timestamp, string category, string name, string symbol, long delta, bool publish = true) {
            IReadOnlyList<MaterialItemData> Update(IReadOnlyList<MaterialItemData> source) {
                var items = source.ToList();
                var index = items.FindIndex(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
                var count = Math.Max(0, (index >= 0 ? items[index].Count : 0) + delta);
                var known = index >= 0 ? items[index] : NewMaterial(name, symbol, 0, category);
                if (index >= 0) { items.RemoveAt(index); }
                if (count > 0) {
                    items.Add(new MaterialItemData { Name = name, Count = count, Category = category, Grade = known.Grade, Maximum = known.Maximum });
                }
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

                // Cargo held changes which local prices the trade panel shows.
                UpdateTrade(currentCargo.LastUpdate, () => {
                    cargoBySymbol.Clear();
                    foreach (var item in json["Inventory"] ?? new JArray()) {
                        string key = CommodityKey(item["Name"]?.Value<string>());
                        if (key.Length == 0) { continue; }
                        long count = (cargoBySymbol.TryGetValue(key, out var held) ? held.Count : 0) + (item["Count"]?.Value<long>() ?? 0);
                        cargoBySymbol[key] = (item["Name_Localised"]?.Value<string>() ?? JournalDisplayText.Format(item["Name"]?.Value<string>()), count);
                    }
                });
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
            // Skimmer and on-foot bounties carry Reward rather than TotalReward.
            long reward = combatData.TotalReward > 0 ? combatData.TotalReward : combatData.Reward;
            UpdateCombatEarnings(combatData.Timestamp, () => {
                unredeemedBounties += reward;
                bountyCount++;
            });
            // Bounties replayed at startup are history, not recent activity.
            if (hydratingCombatEarnings) { return; }
            CoreServer.GameDataEvent(GameEventType.Combat, new CombatData {
                LastUpdate = combatData.Timestamp, Operation = "Bounty", Target = JournalDisplayText.Format(combatData.Target.ToString()),
                Reward = reward, Faction = combatData.VictimFaction
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
