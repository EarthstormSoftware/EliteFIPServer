using EliteFIPProtocol;
using EliteFIPServer.Logging;
using Matric.Integration;
using System.Runtime.CompilerServices;

namespace EliteFIPServer {
    public class MatricApiClient {

        public ComponentState CurrentState { get; private set; } = new ComponentState();
        public event EventHandler<IReadOnlyList<MatricClientSummary>> ConnectedClientsChanged;
        public event EventHandler<MatricClientSummary> ClientAdded;
        public event EventHandler<MatricClientSummary> ClientRemoved;
        public event EventHandler<string> ConnectionFailed;

        public List<ClientInfo> ConnectedClients = new List<ClientInfo>();
        private Dictionary<string, MatricButton> MatricButtonList;

        private string AppName = "Elite FIP Server";
        private string CLIENT_ID;
        private Matric.Integration.Matric matric;
        private bool previousInMainShip;
        private bool previousInFighter;
        private bool previousInSRV;
        private bool previousOnFoot;
        private bool previousHardpointsDeployed;
        private readonly Dictionary<string, string> appliedDeckIds = new();
        

        // Matric Flash Worker
        private CancellationTokenSource MatricFlashWorkerCTS;
        private Task MatricFlashWorkerTask;

        // Guards against a connection attempt (e.g. a misconfigured port) that never raises
        // OnConnectedClientsReceived or OnError, which would otherwise leave CurrentState stuck
        // at Starting forever with no way to Stop or restart the integration.
        private CancellationTokenSource connectWatchdogCts;

        public MatricApiClient() {
            MatricButtonList = CreateButtonList();
        }

        private static Dictionary<string, MatricButton> CreateButtonList() {
            var buttonlist = new Dictionary<string, MatricButton>();

            // Create Button List 
            // For reference:
            // public MatricButton(string buttonName, string buttonLabel, bool isButton = true, bool isIndicator = true, bool isWarning = true , bool isSwitch = true, bool isSlider = false, bool isText = false, bool isPanel = false, 
            //                     string offText = "Off", string onText = "On", bool buttonState = false, int switchPosition = 1, int sliderPosition = 0)
            var templist = new List<MatricButton> {
                new MatricButton(MatricConstants.DOCKED, "Docked", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.LANDED, "Landed", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.LANDINGGEAR, "Landing Gear", offText: "Landing Gear", onText: "Landing Gear"),
                new MatricButton(MatricConstants.SHIELDS, "Shields", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.SUPERCRUISE, "Supercruise", offText: "Supercruise", onText: "Supercruise"),
                new MatricButton(MatricConstants.FLIGHTASSIST, "Flight Assist", offText: "Flight Assist", onText: "Flight Assist"),
                new MatricButton(MatricConstants.HARDPOINTS, "Hardpoints", offText: "Hardpoints", onText: "Hardpoints"),
                new MatricButton(MatricConstants.INWING, "Wing", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.LIGHTS, "Lights", offText: "Lights", onText: "Lights"),
                new MatricButton(MatricConstants.CARGOSCOOP, "Cargo Scoop", offText: "Cargo Scoop", onText: "Cargo Scoop"),
                new MatricButton(MatricConstants.SILENTRUNNING, "Silent Running", offText: "Silent Running", onText: "Silent Running"),
                new MatricButton(MatricConstants.SCOOPINGFUEL, "Scooping Fuel", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.SRVHANDBRAKE, "SRV Handbrake", offText: "SRV Handbrake", onText: "SRV Handbrake"),
                new MatricButton(MatricConstants.SRVTURRET, "SRV Turret", offText: "SRV Turret", onText: "SRV Turret"),
                new MatricButton(MatricConstants.SRVUNDERSHIP, "SRV Under Ship", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.SRVDRIVEASSIST, "SRV DriveAssist", offText: "SRV DriveAssist", onText: "SRV DriveAssist"),
                new MatricButton(MatricConstants.FSDMASSLOCK, "Mass Locked", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.FSDCHARGE, "FSD Charging", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.FSDCOOLDOWN, "FSD Cooldown", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.LOWFUEL, "Low Fuel", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.OVERHEAT, "Overheat", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.INDANGER, "Danger", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.INTERDICTION, "Interdiction", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.INMAINSHIP, "In Main Ship", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.INFIGHTER, "In Fighter", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.INSRV, "In SRV", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.HUDMODE, "HUD Mode", offText: "Combat", onText: "Analysis"),
                new MatricButton(MatricConstants.NIGHTVISION, "Night Vision", offText: "Night Vision", onText: "Night Vision"),
                new MatricButton(MatricConstants.FSDJUMP, "FSD Jump", offText: "FSD Jump", onText: "FSD Jump"),
                new MatricButton(MatricConstants.SRVHIGHBEAM, "SRV High Beam", offText: "SRV High Beam", onText: "SRV High Beam"),

                new MatricButton(MatricConstants.ONFOOT, "On Foot", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.INTAXI, "In Taxi", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.INMULTICREW, "In Multicrew", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.ONFOOTINSTATION, "On Foot In Station", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.ONFOOTONPLANET, "On Foot On Planet", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.AIMDOWNSIGHT, "Aim Down Sight", offText: "Sights", onText: "Sights"),
                new MatricButton(MatricConstants.LOWOXYGEN, "Low Oxygen", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.LOWHEALTH, "Low Health", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.COLD, "Cold", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.HOT, "Hot", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.VERYCOLD, "Very Cold", isButton: false, isSwitch: false),
                new MatricButton(MatricConstants.VERYHOT, "Very Hot", isButton: false, isSwitch: false),

                new MatricButton(MatricConstants.FUELMAIN, "Main Fuel", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isSlider: false, isText: true),
                new MatricButton(MatricConstants.FUELRESERVOIR, "Fuel Reservoir", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isSlider: true, isText: true),

                new MatricButton(MatricConstants.STATUS, "Status", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),
                new MatricButton(MatricConstants.STATUS_LABEL, "Ship Status:", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),

                new MatricButton(MatricConstants.TARGET, "Target", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true),
                new MatricButton(MatricConstants.TARGET_LABEL, "Target Info:", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true)
            };

            foreach (MatricButton button in templist) {
                buttonlist.Add(button.ButtonName, button);
            }
            return buttonlist;
        }

        public void Start() {

            Log.Instance.Info("Starting Matric Integration");
            CurrentState.Set(RunState.Starting);
            ConnectionFailed?.Invoke(this, null);

            if (matric == null) {
                try {
                    matric = new Matric.Integration.Matric(AppName, "", Properties.Settings.Default.MatricApiPort);
                    matric.OnConnectedClientsReceived += Matric_OnConnectedClientsReceived;
                    matric.OnError += Matric_OnError;
                } catch (Exception e) {
                    Log.Instance.Info("Matric Exception: {exception}", e.ToString());
                    CurrentState.Set(RunState.Stopped);
                    ConnectionFailed?.Invoke(this, DescribeConnectionFailure(e));
                    return;
                }
            }

            // There is a possible timing window where an exception will occur after a connection attempt, but before we get here.
            // In that event, matric integration will already have been stopped and matric set to null, so we need to guard against this
            // but no further action need be triggered.
            if (matric != null) {
                RequestConnectedClients();
                StartConnectWatchdog();
            }
        }

        private void StartConnectWatchdog() {
            connectWatchdogCts?.Cancel();
            connectWatchdogCts = new CancellationTokenSource();
            CancellationToken token = connectWatchdogCts.Token;
            TimeSpan timeout = TimeSpan.FromSeconds(Math.Max(Properties.Settings.Default.MatricRetryInterval, 5));

            Task.Delay(timeout, token).ContinueWith(t => {
                if (t.IsCanceled) {
                    return;
                }
                if (CurrentState.State == RunState.Starting) {
                    Log.Instance.Warn("Matric connection attempt timed out after {timeout}s; resetting to Stopped", timeout.TotalSeconds);
                    TeardownMatricClient();
                    CurrentState.Set(RunState.Stopped);
                    ConnectionFailed?.Invoke(this, $"No response from Matric within {timeout.TotalSeconds:0}s. Check the configured port and that Matric is running.");
                }
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        }

        private void StopConnectWatchdog() {
            connectWatchdogCts?.Cancel();
            connectWatchdogCts = null;
        }

        // The vendored Matric client owns a bound socket (for the local Matric API port) that is only
        // released by calling its own Stop()/Dispose() - simply dropping our reference leaves the socket
        // held until the GC finalizes it, which is what caused "address already in use" when the user
        // stopped and immediately restarted the integration.
        //
        // Detaching (nulling the field, unhooking events) always happens synchronously so a subsequent
        // Start() never races against a client we're in the middle of tearing down. The actual
        // Stop()/Dispose() calls can be deferred to a background thread when we're being torn down from
        // inside one of the client's own event callbacks, since calling back into it re-entrantly from
        // that same call stack risks a deadlock if it waits on its own worker thread.
        private Matric.Integration.Matric DetachMatricClient() {
            Matric.Integration.Matric client = matric;
            if (client == null) {
                return null;
            }

            matric = null;
            client.OnConnectedClientsReceived -= Matric_OnConnectedClientsReceived;
            client.OnError -= Matric_OnError;
            return client;
        }

        private static void DisposeMatricClient(Matric.Integration.Matric client) {
            if (client == null) {
                return;
            }

            try {
                client.Stop();
            } catch (Exception e) {
                Log.Instance.Warn("Matric client Stop exception: {exception}", e.ToString());
            }

            try {
                client.Dispose();
            } catch (Exception e) {
                Log.Instance.Warn("Matric client Dispose exception: {exception}", e.ToString());
            }
        }

        private void TeardownMatricClient() {
            DisposeMatricClient(DetachMatricClient());
        }

        private void CompleteStart() {

            StopConnectWatchdog();

            // Start Matric Flash Thread
            Log.Instance.Info("Starting Matric Flash Thread");
            MatricFlashWorkerCTS = new CancellationTokenSource();
            MatricFlashWorkerTask = new Task(new Action(MatricFlashWorkerThread), MatricFlashWorkerCTS.Token);
            MatricFlashWorkerTask.ContinueWith(MatricFlashWorkerThreadEnded);
            MatricFlashWorkerTask.Start();

            Log.Instance.Info("Refreshing Matric button text config");
            foreach (MatricButtonTextConfig buttonConfig in MatricButtonTextConfigStore.Load()) {
                if (MatricButtonList.ContainsKey(buttonConfig.ButtonName)) {
                    MatricButtonList[buttonConfig.ButtonName].OffText = buttonConfig.OffText;
                    MatricButtonList[buttonConfig.ButtonName].OnText = buttonConfig.OnText;
                    MatricButtonList[buttonConfig.ButtonName].UpdateButtonText = buttonConfig.UpdateButtonText;
                }
            }
            CurrentState.Set(RunState.Started);
        }

        public void Stop() {
            Log.Instance.Info("Stopping Matric Integration");
            StopConnectWatchdog();
            if (CurrentState.State == RunState.Started || CurrentState.State == RunState.Starting) {
                CurrentState.Set(RunState.Stopping);
                MatricFlashWorkerCTS?.Cancel();
                if (MatricFlashWorkerTask != null && !MatricFlashWorkerTask.Wait(TimeSpan.FromSeconds(1))) {
                    Log.Instance.Warn("Matric Flash Thread did not stop within the shutdown timeout");
                }
            }
            TeardownMatricClient();
            previousInMainShip = false;
            previousInFighter = false;
            previousInSRV = false;
            previousOnFoot = false;
            previousHardpointsDeployed = false;
            appliedDeckIds.Clear();
            CurrentState.Set(RunState.Stopped);
        }

        public void UpdateGameState(GameEventType eventType, Object gameData) {
            // Only update if Matric Integration is running
            if (CurrentState.State == RunState.Started) {
                if (eventType == GameEventType.Status) {
                    StatusData currentStatus = gameData as StatusData;                   
                    UpdateStatus(currentStatus);
                    
                } else if (eventType == GameEventType.Target) {
                    ShipTargetedData currentTarget = gameData as ShipTargetedData;                   
                    UpdateTarget(currentTarget);                    
                }
            }
        }

        public void Matric_OnConnectedClientsReceived(object source, List<ClientInfo> clients) {
            Log.Instance.Info("Matric client list updated: {clientcount} clients", clients?.Count ?? 0);

            // If we get a client list (even empty) from Matric, we know we have connectivity
            if (CurrentState.State == RunState.Starting) {
                CompleteStart();                
            }
            
            List<ClientInfo> previousClients = ConnectedClients;
            ConnectedClients = clients ?? new List<ClientInfo>();
            var previousByKey = previousClients.ToDictionary(GetClientKey);
            var currentByKey = ConnectedClients.ToDictionary(GetClientKey);

            foreach (var client in ConnectedClients.Where(client => !previousByKey.ContainsKey(GetClientKey(client))))
            {
                ClientAdded?.Invoke(this, ToClientSummary(client));
            }

            foreach (var client in previousClients.Where(client => !currentByKey.ContainsKey(GetClientKey(client))))
            {
                ClientRemoved?.Invoke(this, ToClientSummary(client));
            }

            ConnectedClientsChanged?.Invoke(this, GetConnectedClientSummaries());

            // Matric version 2 supports use of 'null' Client IDs, in which case the updates are set to all Clients. 
            // Previous logic to select first client, and store the ID for reuse is removed in favour of updating all.
            // But we can still log connected clients for info.
            CLIENT_ID = null;
            if (ConnectedClients.Count == 0) {
                Log.Instance.Info("No clients connected");
            } else {
                foreach (ClientInfo clientInfo in ConnectedClients) {
                    Log.Instance.Info("Client name: {name}, IP: {ip}, ID: {id}", clientInfo.Name, clientInfo.IP, clientInfo.Id);
                }
            }
        }

        private void Matric_OnError(Exception ex) {
            Log.Instance.Info("Matric Exception: {message}\r\n{exception}", ex.Message, ex.ToString());
            if (ex is System.Net.Sockets.SocketException) {
                if (ex.HResult == 10054) {
                    System.Threading.Thread.Sleep(Properties.Settings.Default.MatricRetryInterval*1000);
                    RequestConnectedClients();
                    return;
                }
            }

            // Any other error (e.g. connection refused because of a misconfigured port) means the
            // connection attempt has failed outright. Tear down and return to Stopped so the user
            // isn't left with a spinner that never stops and a Start/Stop control they can't use.
            if (CurrentState.State == RunState.Starting) {
                StopConnectWatchdog();
                Matric.Integration.Matric client = DetachMatricClient();
                CurrentState.Set(RunState.Stopped);
                ConnectionFailed?.Invoke(this, DescribeConnectionFailure(ex));
                // This handler runs on the vendored client's own callback thread; defer the actual
                // Stop()/Dispose() calls so we don't call back into it re-entrantly from that stack.
                Task.Run(() => DisposeMatricClient(client));
            }
        }

        private static string DescribeConnectionFailure(Exception ex) {
            if (ex is System.Net.Sockets.SocketException socketEx && socketEx.HResult == 10061) {
                return $"Connection refused on port {Properties.Settings.Default.MatricApiPort}. Check the configured Matric port and that Matric is running.";
            }
            return $"Unable to connect to Matric: {ex.Message}";
        }

        private void MatricFlashWorkerThread() {
            Log.Instance.Info("Matric Flash Worker Thread started");

            CancellationToken token = MatricFlashWorkerCTS.Token;
            while (token.IsCancellationRequested == false) {
                List<SetButtonsVisualStateArgs> buttons = new List<SetButtonsVisualStateArgs>();
                foreach (MatricButton button in MatricButtonList.Values) {
                    if (button != null && button.IsWarning && button.GameState) {
                        buttons.Add(new SetButtonsVisualStateArgs(null, button.ButtonState ? "off" : "on", MatricConstants.WRN + button.ButtonName));
                        button.ButtonState = !button.ButtonState;
                    }
                }
                if (buttons.Count > 0 && matric != null) {
                    matric.SetButtonsVisualState(CLIENT_ID, buttons);
                }
                if (token.WaitHandle.WaitOne(500)) {
                    break;
                }
            }
            Log.Instance.Info("Matric Flash Worker Thread ending");
        }

        private void MatricFlashWorkerThreadEnded(Task task) {
            if (task.Exception != null) {
                Log.Instance.Info("Matric Flash Worker Thread Exception: {exception}", task.Exception.ToString());
            }
            if (CurrentState.State != RunState.Stopping) {
                Stop();
            }            
            Log.Instance.Info("Matric Flash Worker Thread ended");
        }


        public List<ClientInfo> GetConnectedClients() {
            return ConnectedClients;
        }

        public IReadOnlyList<MatricClientSummary> GetConnectedClientSummaries() {
            return ConnectedClients?
                .Select(ToClientSummary)
                .ToList() ?? new List<MatricClientSummary>();
        }

        private static string GetClientKey(ClientInfo client)
        {
            return client.Id ?? $"{client.Name}|{client.IP}";
        }

        private static MatricClientSummary ToClientSummary(ClientInfo client)
        {
            return new MatricClientSummary {
                Name = client.Name,
                IP = client.IP,
                Id = client.Id
            };
        }

        public void RequestConnectedClients() {
            if (matric == null) {
                Log.Instance.Info("Unable to refresh Matric clients because Matric integration is not initialized");
                ConnectedClientsChanged?.Invoke(this, GetConnectedClientSummaries());
                return;
            }

            Log.Instance.Info("Requesting Matric client list");
            matric.GetConnectedClients();
        }

        public void UpdateStatus(StatusData currentStatus) {

            if (currentStatus != null) {
                Log.Instance.Info("Setting Matric state using: {gamestate}", System.Text.Json.JsonSerializer.Serialize(currentStatus));

                // Handle Indicators / Warnings first
                if (MatricButtonList.ContainsKey(MatricConstants.DOCKED)) { MatricButtonList[MatricConstants.DOCKED].GameState = currentStatus.Docked; }
                if (MatricButtonList.ContainsKey(MatricConstants.LANDED)) { MatricButtonList[MatricConstants.LANDED].GameState = currentStatus.Landed; }
                if (MatricButtonList.ContainsKey(MatricConstants.SHIELDS)) { MatricButtonList[MatricConstants.SHIELDS].GameState = !currentStatus.ShieldsUp; }
                if (MatricButtonList.ContainsKey(MatricConstants.INWING)) { MatricButtonList[MatricConstants.INWING].GameState = currentStatus.InWing; }
                if (MatricButtonList.ContainsKey(MatricConstants.SCOOPINGFUEL)) { MatricButtonList[MatricConstants.SCOOPINGFUEL].GameState = currentStatus.ScoopingFuel; }
                if (MatricButtonList.ContainsKey(MatricConstants.SRVUNDERSHIP)) { MatricButtonList[MatricConstants.SRVUNDERSHIP].GameState = currentStatus.SrvUnderShip; }
                if (MatricButtonList.ContainsKey(MatricConstants.FSDMASSLOCK)) { MatricButtonList[MatricConstants.FSDMASSLOCK].GameState = currentStatus.FsdMassLocked; }
                if (MatricButtonList.ContainsKey(MatricConstants.FSDCHARGE)) { MatricButtonList[MatricConstants.FSDCHARGE].GameState = currentStatus.FsdCharging; }
                if (MatricButtonList.ContainsKey(MatricConstants.FSDCOOLDOWN)) { MatricButtonList[MatricConstants.FSDCOOLDOWN].GameState = currentStatus.FsdCooldown; }
                if (MatricButtonList.ContainsKey(MatricConstants.LOWFUEL)) { MatricButtonList[MatricConstants.LOWFUEL].GameState = currentStatus.LowFuel; }
                if (MatricButtonList.ContainsKey(MatricConstants.OVERHEAT)) { MatricButtonList[MatricConstants.OVERHEAT].GameState = currentStatus.Overheating; }
                if (MatricButtonList.ContainsKey(MatricConstants.INDANGER)) { MatricButtonList[MatricConstants.INDANGER].GameState = currentStatus.InDanger; }
                if (MatricButtonList.ContainsKey(MatricConstants.INTERDICTION)) { MatricButtonList[MatricConstants.INTERDICTION].GameState = currentStatus.BeingInterdicted; }
                if (MatricButtonList.ContainsKey(MatricConstants.INMAINSHIP)) { MatricButtonList[MatricConstants.INMAINSHIP].GameState = currentStatus.InMainShip; }
                if (MatricButtonList.ContainsKey(MatricConstants.INFIGHTER)) { MatricButtonList[MatricConstants.INFIGHTER].GameState = currentStatus.InFighter; }
                if (MatricButtonList.ContainsKey(MatricConstants.INSRV)) { MatricButtonList[MatricConstants.INSRV].GameState = currentStatus.InSRV; }

                if (MatricButtonList.ContainsKey(MatricConstants.ONFOOT)) { MatricButtonList[MatricConstants.ONFOOT].GameState = currentStatus.OnFoot; }
                if (MatricButtonList.ContainsKey(MatricConstants.INTAXI)) { MatricButtonList[MatricConstants.INTAXI].GameState = currentStatus.InTaxi; }
                if (MatricButtonList.ContainsKey(MatricConstants.INMULTICREW)) { MatricButtonList[MatricConstants.INMULTICREW].GameState = currentStatus.InMulticrew; }
                if (MatricButtonList.ContainsKey(MatricConstants.ONFOOTINSTATION)) { MatricButtonList[MatricConstants.ONFOOTINSTATION].GameState = currentStatus.OnFootInStation; }
                if (MatricButtonList.ContainsKey(MatricConstants.ONFOOTONPLANET)) { MatricButtonList[MatricConstants.ONFOOTONPLANET].GameState = currentStatus.OnFootOnPlanet; }
                if (MatricButtonList.ContainsKey(MatricConstants.LOWOXYGEN)) { MatricButtonList[MatricConstants.LOWOXYGEN].GameState = currentStatus.LowOxygen; }
                if (MatricButtonList.ContainsKey(MatricConstants.LOWHEALTH)) { MatricButtonList[MatricConstants.LOWHEALTH].GameState = currentStatus.LowHealth; }
                if (MatricButtonList.ContainsKey(MatricConstants.COLD)) { MatricButtonList[MatricConstants.COLD].GameState = currentStatus.Cold; }
                if (MatricButtonList.ContainsKey(MatricConstants.HOT)) { MatricButtonList[MatricConstants.HOT].GameState = currentStatus.Hot; }
                if (MatricButtonList.ContainsKey(MatricConstants.VERYCOLD)) { MatricButtonList[MatricConstants.VERYCOLD].GameState = currentStatus.VeryCold; }
                if (MatricButtonList.ContainsKey(MatricConstants.VERYHOT)) { MatricButtonList[MatricConstants.VERYHOT].GameState = currentStatus.VeryHot; }

                ApplyPageSwitch(currentStatus);


                // Buttons and switches need extra TLC
                if (MatricButtonList.ContainsKey(MatricConstants.LANDINGGEAR)) {
                    MatricButtonList[MatricConstants.LANDINGGEAR].GameState = currentStatus.LandingGearDown;
                    MatricButtonList[MatricConstants.LANDINGGEAR].SwitchPosition = currentStatus.LandingGearDown ? 1 : 0;
                }
                if (MatricButtonList.ContainsKey(MatricConstants.SUPERCRUISE)) {
                    MatricButtonList[MatricConstants.SUPERCRUISE].GameState = currentStatus.Supercruise;
                    MatricButtonList[MatricConstants.SUPERCRUISE].SwitchPosition = currentStatus.Supercruise ? 1 : 0;
                }
                if (MatricButtonList.ContainsKey(MatricConstants.FLIGHTASSIST)) {
                    MatricButtonList[MatricConstants.FLIGHTASSIST].GameState = currentStatus.FlightAssistOff;
                    MatricButtonList[MatricConstants.FLIGHTASSIST].SwitchPosition = currentStatus.FlightAssistOff ? 0 : 1;
                }
                if (MatricButtonList.ContainsKey(MatricConstants.HARDPOINTS)) {
                    MatricButtonList[MatricConstants.HARDPOINTS].GameState = currentStatus.HardpointsDeployed;
                    MatricButtonList[MatricConstants.HARDPOINTS].SwitchPosition = currentStatus.HardpointsDeployed ? 1 : 0;
                }
                if (MatricButtonList.ContainsKey(MatricConstants.LIGHTS)) {
                    MatricButtonList[MatricConstants.LIGHTS].GameState = currentStatus.LightsOn;
                    MatricButtonList[MatricConstants.LIGHTS].SwitchPosition = currentStatus.LightsOn ? 1 : 0;
                }
                if (MatricButtonList.ContainsKey(MatricConstants.CARGOSCOOP)) {
                    MatricButtonList[MatricConstants.CARGOSCOOP].GameState = currentStatus.CargoScoopDeployed;
                    MatricButtonList[MatricConstants.CARGOSCOOP].SwitchPosition = currentStatus.CargoScoopDeployed ? 1 : 0;
                }
                if (MatricButtonList.ContainsKey(MatricConstants.SILENTRUNNING)) {
                    MatricButtonList[MatricConstants.SILENTRUNNING].GameState = currentStatus.SilentRunning;
                    MatricButtonList[MatricConstants.SILENTRUNNING].SwitchPosition = currentStatus.SilentRunning ? 1 : 0;
                }
                if (MatricButtonList.ContainsKey(MatricConstants.SRVHANDBRAKE)) {
                    MatricButtonList[MatricConstants.SRVHANDBRAKE].GameState = currentStatus.SrvHandbrake;
                    MatricButtonList[MatricConstants.SRVHANDBRAKE].SwitchPosition = currentStatus.SrvHandbrake ? 1 : 0;
                }
                if (MatricButtonList.ContainsKey(MatricConstants.SRVTURRET)) {
                    MatricButtonList[MatricConstants.SRVTURRET].GameState = currentStatus.SrvTurret;
                    MatricButtonList[MatricConstants.SRVTURRET].SwitchPosition = currentStatus.SrvTurret ? 1 : 0;
                }
                if (MatricButtonList.ContainsKey(MatricConstants.SRVDRIVEASSIST)) {
                    MatricButtonList[MatricConstants.SRVDRIVEASSIST].GameState = currentStatus.SrvDriveAssist;
                    MatricButtonList[MatricConstants.SRVDRIVEASSIST].SwitchPosition = currentStatus.SrvDriveAssist ? 1 : 0;
                }
                if (MatricButtonList.ContainsKey(MatricConstants.HUDMODE)) {
                    MatricButtonList[MatricConstants.HUDMODE].GameState = currentStatus.HudAnalysisMode;
                    MatricButtonList[MatricConstants.HUDMODE].SwitchPosition = currentStatus.HudAnalysisMode ? 1 : 0;
                }
                if (MatricButtonList.ContainsKey(MatricConstants.NIGHTVISION)) {
                    MatricButtonList[MatricConstants.NIGHTVISION].GameState = currentStatus.NightVision;
                    MatricButtonList[MatricConstants.NIGHTVISION].SwitchPosition = currentStatus.NightVision ? 1 : 0;
                }
                if (MatricButtonList.ContainsKey(MatricConstants.FSDJUMP)) {
                    MatricButtonList[MatricConstants.FSDJUMP].GameState = currentStatus.FsdJump;
                    MatricButtonList[MatricConstants.FSDJUMP].SwitchPosition = currentStatus.FsdJump ? 1 : 0;
                }
                if (MatricButtonList.ContainsKey(MatricConstants.SRVHIGHBEAM)) {
                    MatricButtonList[MatricConstants.SRVHIGHBEAM].GameState = currentStatus.SrvHighBeam;
                    MatricButtonList[MatricConstants.SRVHIGHBEAM].SwitchPosition = currentStatus.SrvHighBeam ? 1 : 0;
                }

                if (MatricButtonList.ContainsKey(MatricConstants.AIMDOWNSIGHT)) {
                    MatricButtonList[MatricConstants.AIMDOWNSIGHT].GameState = currentStatus.AimDownSight;
                    MatricButtonList[MatricConstants.AIMDOWNSIGHT].SwitchPosition = currentStatus.AimDownSight ? 1 : 0;
                }

                // Handle Sliders and text fields
                if (MatricButtonList.ContainsKey(MatricConstants.FUELMAIN)) {
                    MatricButtonList[MatricConstants.FUELMAIN].OffText = Math.Round((decimal)currentStatus.FuelReservoir, 2).ToString();
                }

                if (MatricButtonList.ContainsKey(MatricConstants.FUELRESERVOIR)) {
                    MatricButtonList[MatricConstants.FUELRESERVOIR].OffText = Math.Round((decimal)currentStatus.FuelReservoir, 2).ToString();

                    // Set slider position, and handle data oddities
                    if (Math.Round((decimal)currentStatus.FuelReservoir, 2) >= 1) {
                        MatricButtonList[MatricConstants.FUELRESERVOIR].SliderPosition = 100;
                    } else if (Math.Round((decimal)currentStatus.FuelReservoir, 2) <= 0) {
                        MatricButtonList[MatricConstants.FUELRESERVOIR].SliderPosition = 0;
                    } else {
                        MatricButtonList[MatricConstants.FUELRESERVOIR].SliderPosition = (int)Math.Round((decimal)currentStatus.FuelReservoir, 2) * 100;
                    }
                }


                // Handle Special Text fields
                if (MatricButtonList.ContainsKey(MatricConstants.STATUS_LABEL)) {
                    MatricButtonList[MatricConstants.STATUS_LABEL].OffText = FormatStatusLabel(currentStatus);
                }
                if (MatricButtonList.ContainsKey(MatricConstants.STATUS)) {
                    MatricButtonList[MatricConstants.STATUS].OffText = FormatStatusText(currentStatus);
                }

                foreach (MatricButton button in MatricButtonList.Values) {
                    if (button != null) {
                        button.UpdateMatricState(matric, CLIENT_ID);
                    }
                }
            }
        }

        private void ApplyPageSwitch(StatusData currentStatus)
        {
            bool enteredMainShip = currentStatus.InMainShip && !previousInMainShip;
            bool enteredFighter = currentStatus.InFighter && !previousInFighter;
            bool enteredSRV = currentStatus.InSRV && !previousInSRV;
            bool enteredOnFoot = currentStatus.OnFoot && !previousOnFoot;
            bool enteredHardpointsDeployed = currentStatus.HardpointsDeployed && !previousHardpointsDeployed;
            bool enteredHardpointsRetracted = !currentStatus.HardpointsDeployed && previousHardpointsDeployed;

            previousInMainShip = currentStatus.InMainShip;
            previousInFighter = currentStatus.InFighter;
            previousInSRV = currentStatus.InSRV;
            previousOnFoot = currentStatus.OnFoot;
            previousHardpointsDeployed = currentStatus.HardpointsDeployed;

            string enteredState = enteredOnFoot ? "OnFoot" :
                enteredSRV ? "InSRV" :
                enteredFighter ? "InFighter" :
                enteredMainShip ? "InMainShip" : null;

            string enteredHardpointsState = enteredHardpointsDeployed ? "HardpointsDeployed" :
                enteredHardpointsRetracted ? "HardpointsRetracted" : null;

            if (enteredState == null && enteredHardpointsState == null)
            {
                return;
            }

            var profiles = MatricClientProfileStore.Load();
            foreach (var client in ConnectedClients)
            {
                var profile = profiles.FirstOrDefault(item => item.ClientId == client.Id);
                if (enteredState != null)
                {
                    SwitchToConfiguredPage(profile, enteredState, client.Id);
                }
                if (enteredHardpointsState != null)
                {
                    SwitchToConfiguredPage(profile, enteredHardpointsState, client.Id);
                }
            }
        }

        private void SwitchToConfiguredPage(MatricClientProfile profile, string state, string clientId)
        {
            var config = profile?.PageSwitches?.FirstOrDefault(item => item.State == state) ??
                MatricPageSwitchConfigStore.Load().FirstOrDefault(item => item.State == state);
            if (config == null || !config.Enabled || string.IsNullOrWhiteSpace(config.PageId) || matric == null)
            {
                return;
            }

            try
            {
                Log.Instance.Info("Switching Matric page for {state} to {pageId}", state, config.PageId);
                if (profile != null && !string.IsNullOrWhiteSpace(profile.DeckId) && appliedDeckIds.GetValueOrDefault(clientId) != profile.DeckId.Trim())
                {
                    matric.SetDeck(clientId, profile.DeckId.Trim(), config.PageId.Trim());
                    appliedDeckIds[clientId] = profile.DeckId.Trim();
                }
                else
                {
                    matric.SetActivePage(clientId, config.PageId.Trim());
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Info("Unable to switch Matric page for {state}: {error}", state, ex.Message);
            }
        }

        public void UpdateTarget(ShipTargetedData currentTarget) {

            if (currentTarget != null) {

                // Handle Text fields
                if (MatricButtonList.ContainsKey(MatricConstants.TARGET_LABEL)) {
                    MatricButtonList[MatricConstants.TARGET_LABEL].OffText = FormatTargetLabel(currentTarget);
                }
                if (MatricButtonList.ContainsKey(MatricConstants.TARGET)) {
                    MatricButtonList[MatricConstants.TARGET].OffText = FormatTargetText(currentTarget);
                }

                foreach (MatricButton button in MatricButtonList.Values) {
                    if (button != null) {
                        button.UpdateMatricState(matric, CLIENT_ID);
                    }
                }
            }
        }

        private static string FormatTargetText(ShipTargetedData targetData) {

            string displayText = "";
            if (targetData != null) {

                if (targetData.TargetLocked == true) {
                    string bountyText = targetData.Bounty == 0 ? "" : targetData.Bounty.ToString();
                    var targetTemplate = $"<table>" +
                        $"<tr><td>{targetData.Ship}</td></tr>" +
                        $"<tr><td>{targetData.PilotName}</td></tr>" +
                        $"<tr><td>{targetData.PilotRank}</td></tr>" +
                        $"<tr><td>{targetData.Faction}</td></tr>" +
                        $"<tr><td>{targetData.LegalStatus}</td></tr>" +
                        $"<tr><td>{bountyText}</td></tr>" +
                        $"</table>";
                    displayText = targetTemplate;
                } else {
                    displayText = "<table><tr><td>No target selected</td></tr></table>";
                }
            }
            return displayText;
        }

        private static string FormatTargetLabel(ShipTargetedData targetData) {

            string displayText = "";
            if (targetData != null) {
                if (targetData.TargetLocked == true) {
                    var targetTemplate = $"<table>" +
                        $"<tr><td>Target:</td></tr>" +
                        $"<tr><td>Pilot:</td></tr>" +
                        $"<tr><td>Rank:</td></tr>" +
                        $"<tr><td>Faction:</td></tr>" +
                        $"<tr><td>Status:</td></tr>" +
                        $"<tr><td>Bounty:</td></tr>" +
                        $"</table>";
                    displayText = targetTemplate;
                } else {

                    displayText = "<table><tr><td>Target:</td></tr></table>";
                }
            }
            return displayText;
        }

        private static string FormatStatusText(StatusData statusData) {

            string displayText = "";
            if (statusData != null) {
                var statusTemplate = $"<table>";

                if (string.IsNullOrEmpty(statusData.BodyName) == true) {
                    statusTemplate = statusTemplate + $"<tr><td>Not available</td></tr>";
                } else {
                    statusTemplate = statusTemplate + $"<tr><td>{statusData.BodyName}</td></tr>";
                }
                if (string.IsNullOrEmpty(statusData.LegalState) == true) {
                    statusTemplate = statusTemplate + $"<tr><td>Unknown</td></tr>";
                } else {
                    statusTemplate = statusTemplate + $"<tr><td>{statusData.LegalState}</td></tr>";
                }

                statusTemplate = statusTemplate + $"<tr><td><br></td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>{statusData.Cargo.ToString()}</td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>{Math.Round((decimal)statusData.FuelMain, 2).ToString()}</td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>{Math.Round((decimal)statusData.FuelReservoir, 2).ToString()}</td></tr>";
                statusTemplate = statusTemplate + $"</table>";
                displayText = statusTemplate;
            }
            return displayText;
        }

        private static string FormatStatusLabel(StatusData statusData) {

            string displayText = "";
            if (statusData != null) {
                var statusTemplate = $"<table>";

                statusTemplate = statusTemplate + $"<tr><td>Closest body:</td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>Legal state:</td></tr>";
                statusTemplate = statusTemplate + $"<tr><td><br></td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>Cargo:</td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>Main fuel:</td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>Fuel reservoir:</td></tr>";
                statusTemplate = statusTemplate + $"</table>";
                displayText = statusTemplate;
            }
            return displayText;
        }
    }
}
