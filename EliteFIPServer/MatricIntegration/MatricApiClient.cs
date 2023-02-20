using EliteFIPProtocol;
using EliteFIPServer.Logging;
using Matric.Integration;
using Newtonsoft.Json;
using System.IO;

namespace EliteFIPServer.MatricIntegration {
    public class MatricApiClient {

        private static ServerCore ServerCore;
        
        public static List<ClientInfo> ConnectedClients = new List<ClientInfo>();
        private static Dictionary<string, MatricButton> MatricButtonList = CreateButtonList();

        private string AppName = "Elite FIP Server";
        private static string CLIENT_ID;
        private static Matric.Integration.Matric matric;        
        private static State ApiClientState = State.Stopped;

        // Matric Flash Worker
        private CancellationTokenSource MatricFlashWorkerCTS;
        private Task MatricFlashWorkerTask;

        public MatricApiClient(ServerCore serverCore) {
            ServerCore = serverCore;
        }

        private static Dictionary<string, MatricButton> CreateButtonList() {
            var buttonlist = new Dictionary<string, MatricButton>();
            var templist = new List<MatricButton>();

            // Create Button List 
            // For reference:
            // public MatricButton(string buttonName, string buttonLabel, bool isButton = true, bool isIndicator = true, bool isWarning = true , bool isSwitch = true, bool isSlider = false, bool isText = false, bool isPanel = false, 
            //                     string offText = "Off", string onText = "On", bool buttonState = false, int switchPosition = 1, int sliderPosition = 0)

            templist.Add(new MatricButton(MatricConstants.DOCKED, "Docked", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.LANDED, "Landed", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.LANDINGGEAR, "Landing Gear", offText: "Landing Gear", onText: "Landing Gear"));
            templist.Add(new MatricButton(MatricConstants.SHIELDS, "Shields", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.SUPERCRUISE, "Supercruise", offText: "Supercruise", onText: "Supercruise"));
            templist.Add(new MatricButton(MatricConstants.FLIGHTASSIST, "Flight Assist", offText: "Flight Assist", onText: "Flight Assist"));
            templist.Add(new MatricButton(MatricConstants.HARDPOINTS, "Hardpoints", offText: "Hardpoints", onText: "Hardpoints"));
            templist.Add(new MatricButton(MatricConstants.INWING, "Wing", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.LIGHTS, "Lights", offText: "Lights", onText: "Lights"));
            templist.Add(new MatricButton(MatricConstants.CARGOSCOOP, "Cargo Scoop", offText: "Cargo Scoop", onText: "Cargo Scoop"));
            templist.Add(new MatricButton(MatricConstants.SILENTRUNNING, "Silent Running", offText: "Silent Running", onText: "Silent Running"));
            templist.Add(new MatricButton(MatricConstants.SCOOPINGFUEL, "Scooping Fuel", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.SRVHANDBRAKE, "SRV Handbrake", offText: "SRV Handbrake", onText: "SRV Handbrake"));
            templist.Add(new MatricButton(MatricConstants.SRVTURRET, "SRV Turret", offText: "SRV Turret", onText: "SRV Turret"));
            templist.Add(new MatricButton(MatricConstants.SRVUNDERSHIP, "SRV Under Ship", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.SRVDRIVEASSIST, "SRV DriveAssist", offText: "SRV DriveAssist", onText: "SRV DriveAssist"));
            templist.Add(new MatricButton(MatricConstants.FSDMASSLOCK, "Mass Locked", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.FSDCHARGE, "FSD Charging", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.FSDCOOLDOWN, "FSD Cooldown", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.LOWFUEL, "Low Fuel", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.OVERHEAT, "Overheat", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.INDANGER, "Danger", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.INTERDICTION, "Interdiction", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.INMAINSHIP, "In Main Ship", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.INFIGHTER, "In Fighter", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.INSRV, "In SRV", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.HUDMODE, "HUD Mode", offText: "Combat", onText: "Analysis"));
            templist.Add(new MatricButton(MatricConstants.NIGHTVISION, "Night Vision", offText: "Night Vision", onText: "Night Vision"));
            templist.Add(new MatricButton(MatricConstants.FSDJUMP, "FSD Jump", offText: "FSD Jump", onText: "FSD Jump"));
            templist.Add(new MatricButton(MatricConstants.SRVHIGHBEAM, "SRV High Beam", offText: "SRV High Beam", onText: "SRV High Beam"));

            templist.Add(new MatricButton(MatricConstants.ONFOOT, "On Foot", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.INTAXI, "In Taxi", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.INMULTICREW, "In Multicrew", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.ONFOOTINSTATION, "On Foot In Station", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.ONFOOTONPLANET, "On Foot On Planet", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.AIMDOWNSIGHT, "Aim Down Sight", offText: "Sights", onText: "Sights"));
            templist.Add(new MatricButton(MatricConstants.LOWOXYGEN, "Low Oxygen", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.LOWHEALTH, "Low Health", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.COLD, "Cold", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.HOT, "Hot", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.VERYCOLD, "Very Cold", isButton: false, isSwitch: false));
            templist.Add(new MatricButton(MatricConstants.VERYHOT, "Very Hot", isButton: false, isSwitch: false));

            templist.Add(new MatricButton(MatricConstants.FUELMAIN, "Main Fuel", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isSlider: false, isText: true));
            templist.Add(new MatricButton(MatricConstants.FUELRESERVOIR, "Fuel Reservoir", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isSlider: true, isText: true));

            templist.Add(new MatricButton(MatricConstants.STATUS, "Status", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true));
            templist.Add(new MatricButton(MatricConstants.STATUS_LABEL, "Ship Status:", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true));

            templist.Add(new MatricButton(MatricConstants.TARGET, "Target", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true));
            templist.Add(new MatricButton(MatricConstants.TARGET_LABEL, "Target Info:", isButton: false, isIndicator: false, isWarning: false, isSwitch: false, isText: true));

            foreach (MatricButton button in templist) {
                buttonlist.Add(button.ButtonName, button);
            }
            return buttonlist;
        }

        public void Start() {

            Log.Instance.Info("Starting Matric Integration");
            ApiClientState = State.Starting;            
            if (matric == null) {
                try {
                    matric = new Matric.Integration.Matric(AppName, "", Properties.Settings.Default.MatricApiPort);
                    matric.OnConnectedClientsReceived += Matric_OnConnectedClientsReceived;
                    matric.OnError += Matric_OnError;
                } catch (Exception e) {
                    Log.Instance.Info("Matric Exception: {exception}", e.ToString());
                }
            }

            // Refesh Button Text Config 
            Log.Instance.Info("Refesh Button Text Config");
            try {
                string jsonButtonTextConfig = File.ReadAllText(Constants.ButtonTextConfigFilename);
                var buttonTextConfigList = JsonConvert.DeserializeObject<List<ButtonTextConfig>>(jsonButtonTextConfig);
                MatricButtonList.Clear();
                foreach (ButtonTextConfig buttonConfig in buttonTextConfigList) {
                    if (MatricButtonList.ContainsKey(buttonConfig.ButtonName)) {
                        MatricButtonList[buttonConfig.ButtonName].OffText = buttonConfig.OffText;
                        MatricButtonList[buttonConfig.ButtonName].OnText = buttonConfig.OnText;
                        MatricButtonList[buttonConfig.ButtonName].UpdateButtonText = buttonConfig.UpdateButtonText;
                        Log.Instance.Info("Button updated: {name}, Offtext: {offtext}, Ontext: {Ontext},UpdateButtonText: {updatebuttontext}",
                            MatricButtonList[buttonConfig.ButtonName].ButtonName, MatricButtonList[buttonConfig.ButtonName].OffText, MatricButtonList[buttonConfig.ButtonName].OnText, MatricButtonList[buttonConfig.ButtonName].UpdateButtonText);
                    }
                }
            } catch {
                Log.Instance.Info("Unable to refesh Button Text Config");
            }

            // Start Matric Flashing Lights thread
            StartMatricFlashWorker();

            matric.GetConnectedClients();
        }

        public void Stop() {
            Log.Instance.Info("Stopping Matric Integration");
            ApiClientState = State.Stopping;
            StopMatricFlashWorker();
            MatricFlashWorkerTask.Wait();
            matric = null;
        }

        public static void Matric_OnConnectedClientsReceived(object source, List<ClientInfo> clients) {
            Log.Instance.Info("Matric client list updated");

            // If we get a client list (even empty) from Matric, we know we have connectivity
            ApiClientState = State.Started;
            ServerCore.UpdateMatricApiClientState(ApiClientState);
            ConnectedClients = clients;

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

        private static void Matric_OnError(Exception ex) {
            Log.Instance.Info("Matric Exception: {message}\r\n{exception}", ex.Message, ex.ToString());
            ApiClientState = State.Stopped;
            ServerCore.UpdateMatricApiClientState(ApiClientState);
        }

        public List<ClientInfo> GetConnectedClients() {
            return ConnectedClients;
        }

        public bool IsConnected() {
            return ApiClientState == State.Started ? true : false;
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

        public void StartMatricFlashWorker() {
            // Start Matric Flash Thread
            Log.Instance.Info("Starting Matric Flash Thread");
            MatricFlashWorkerCTS = new CancellationTokenSource();
            MatricFlashWorkerTask = new Task(new Action(MatricFlashWorkerThread), MatricFlashWorkerCTS.Token);
            MatricFlashWorkerTask.ContinueWith(MatricFlashWorkerThreadEnded);
            MatricFlashWorkerTask.Start();
        }

        public void StopMatricFlashWorker() {
            // Stop Matric Flash Thread
            Log.Instance.Info("Stopping Matric Flash Thread");
            MatricFlashWorkerCTS.Cancel();
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
                if (buttons.Count > 0) {
                    matric.SetButtonsVisualState(CLIENT_ID, buttons);
                }
                Thread.Sleep(500);
            }
            Log.Instance.Info("Matric Flash Worker Thread ending");
        }

        private void MatricFlashWorkerThreadEnded(Task task) {
            if (task.Exception != null) {
                Log.Instance.Info("Matric Flash Worker Thread Exception: {exception}", task.Exception.ToString());
            } 
            Log.Instance.Info("Matric Flash Worker Thread ended");
            if (ApiClientState != State.Stopped) {
                ApiClientState = State.Stopped;
                ServerCore.UpdateMatricApiClientState(ApiClientState);
            }
        }
    }
}
