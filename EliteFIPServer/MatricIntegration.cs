using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Matric.Integration;
using EliteFIPServer.Logging;
using EliteFIPProtocol;
using System.Text.Json;

namespace EliteFIPServer {
    public class MatricIntegration {
        
        // Switchable Buttons
        public static string GEAR = "btnGear";
        public static string SUPERCRUISE = "btnSupercruise";
        public static string FLIGHTASSIST = "btnFlightAssist";
        public static string HARDPOINTS = "btnHardpoints";
        public static string LIGHTS = "btnLights";
        public static string SCOOP = "btnScoop";        
        public static string SILENT = "btnSilent";
        public static string HUDMODE = "btnHudMode";
        public static string NVISION = "btnNVision";
        public static string FSDJUMP = "btnJump";


        // Indicator Lights
        public static string DOCKED = "indDocked";
        public static string LANDED = "indLanded";
        public static string SHIELDS= "indShields";
        public static string WING = "indInWing";
        public static string SCOOPINGFUEL = "indScoopingFuel";        
        public static string FSDMASSLOCK = "indFSDMassLock";
        public static string FSDCHARGE = "indFSDCharging";
        public static string FSDCOOLDOWN = "indFSDCooldown";
        public static string LOWFUEL = "indLowFuel";
        public static string OVERHEAT = "indOverheat";
        public static string INDANGER = "indInDanger";
        public static string INTERDICTION = "indInterdiction";
        public static string MAINSHIP = "indMainShip";
        public static string FIGHTER = "indIFighter";
        public static string SRV = "indSRV";


        // Text Information
        public static string TARGET = "txtTarget";
        public static string TARGET_LABEL = "txtTargetLabel";
        public static string STATUS = "txtStatus";
        public static string STATUS_LABEL = "txtStatusLabel";

       

        private string WHITE = "#FFFFFF";
        private string ORANGE = "#EC8908";
        
        public static List<ClientInfo> ConnectedClients = new List<ClientInfo>();

        private string AppName = "Elite FIP Server";
        private static string CLIENT_ID;
        private static Matric.Integration.Matric matric;
        private static bool verifiedMatricConnection = false;

        public void Connect() {

            Log.Instance.Info("Connecting to Matric");
            if (matric == null) {
                matric = new Matric.Integration.Matric(AppName);
            }

            if (String.IsNullOrEmpty(matric.PIN) == true) {
                if (String.IsNullOrEmpty(Properties.Settings.Default.MatricPin) == true) {
                    Log.Instance.Info("Requesting Matric PIN");

                    matric.RequestAuthorizePrompt();
                    MatricPinEntry pinEntry = new MatricPinEntry();
                    DialogResult dialogresult = pinEntry.ShowDialog();
                    if (dialogresult == DialogResult.OK) {
                        if (String.IsNullOrEmpty(pinEntry.MatricPin) != true) {
                            Log.Instance.Info("New Matric PIN: {pin}", pinEntry.MatricPin);
                            matric.PIN = pinEntry.MatricPin;
                            Properties.Settings.Default.MatricPin = pinEntry.MatricPin;
                            Properties.Settings.Default.Save();
                            matric.OnConnectedClientsReceived += Matric_OnConnectedClientsReceived;
                            matric.GetConnectedClients();
                        }

                    } else if (dialogresult == DialogResult.Cancel) {

                    }
                    pinEntry.Dispose();

                } else {
                    matric.PIN = Properties.Settings.Default.MatricPin;
                    Log.Instance.Info("Matric PIN: {pin}", matric.PIN);
                    matric.OnConnectedClientsReceived += Matric_OnConnectedClientsReceived;
                    matric.GetConnectedClients();
                }
            }            
        }

        public static void Matric_OnConnectedClientsReceived(object source, List<ClientInfo> clients) {
            Log.Instance.Info("Matric client list updated");

            // If we get a client list (even empty) from Matric, we know we have connectivity (aka the PIN is correct)
            verifiedMatricConnection = true;
            ConnectedClients = clients;            
            if (ConnectedClients.Count == 0) {
                CLIENT_ID = "";
                Log.Instance.Info("No clients connected");
            } else {                
                foreach (ClientInfo clientInfo in ConnectedClients) {
                    Log.Instance.Info("Client name: {name}, IP: {ip}, ID: {id}", clientInfo.Name, clientInfo.IP, clientInfo.Id);
                }
                if (String.IsNullOrEmpty(CLIENT_ID)) {
                    string savedClientId = Properties.Settings.Default.MatricClient;
                    if (String.IsNullOrEmpty(savedClientId)) {                        
                        CLIENT_ID = ConnectedClients[0].Id;
                        Properties.Settings.Default.MatricClient = CLIENT_ID;
                        Properties.Settings.Default.Save();
                        Log.Instance.Info("No saved client found, saving default client as: {clientid}", CLIENT_ID);
                    } else {
                        Log.Instance.Info("Searching for saved client");
                        foreach (ClientInfo clientInfo in ConnectedClients) {
                            if (clientInfo.Id == savedClientId) {
                                CLIENT_ID = savedClientId;
                                Log.Instance.Info("Setting client to: {clientid}", CLIENT_ID);
                                break;
                            }
                        }
                        if (String.IsNullOrEmpty(CLIENT_ID)) {
                            CLIENT_ID = ConnectedClients[0].Id;
                            Log.Instance.Info("Saved client not connected, setting client to: {clientid}", CLIENT_ID);
                        }
                    }
                }
            }
        }

        public List<ClientInfo> GetConnectedClients() {            
            return ConnectedClients;
        }

        public void SetClientId(string clientId) {
            CLIENT_ID = clientId;
        }

        public bool IsConnected() {
            return verifiedMatricConnection;
        }

        public void UpdateStatus(StatusData currentStatus) {
            
            if (currentStatus != null) {                
                Log.Instance.Info("Setting Matric state using: {gamestate}", JsonSerializer.Serialize(currentStatus));
                List<SetButtonsVisualStateArgs> buttons = new List<SetButtonsVisualStateArgs>();

                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.LandingGearDown == true ? "on" : "off" , GEAR));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.Supercruise == true ? "on" : "off", SUPERCRUISE));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.FlightAssistOff == false ? "on" : "off", FLIGHTASSIST));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.HardpointsDeployed == true ? "on" : "off", HARDPOINTS));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.LightsOn == true ? "on" : "off", LIGHTS));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.CargoScoopDeployed == true ? "on" : "off", SCOOP));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.SilentRunning == true ? "on" : "off", SILENT));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.HudAnalysisMode == true ? "on" : "off", HUDMODE));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.NightVision == true ? "on" : "off", NVISION));                
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.FsdJump == true ? "on" : "off", FSDJUMP));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.Docked == true ? "on" : "off", DOCKED));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.Landed == true ? "on" : "off", LANDED));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.ShieldsUp == true ? "on" : "off",SHIELDS));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.InWing == true ? "on" : "off", WING));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.ScoopingFuel == true ? "on" : "off",SCOOPINGFUEL));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.FsdMassLocked == true ? "on" : "off", FSDMASSLOCK));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.FsdCharging == true ? "on" : "off", FSDCHARGE));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.FsdCooldown == true ? "on" : "off", FSDCOOLDOWN));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.LowFuel == true ? "on" : "off", LOWFUEL));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.Overheating == true ? "on" : "off", OVERHEAT));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.InDanger == true ? "on" : "off", INDANGER));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.BeingInterdicted == true ? "on" : "off", INTERDICTION));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.InMainShip == true ? "on" : "off", MAINSHIP));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.InFighter == true ? "on" : "off", FIGHTER));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.InSRV == true ? "on" : "off", SRV));
                matric.SetButtonsVisualState(CLIENT_ID, buttons);

                matric.SetButtonProperties(CLIENT_ID, null, text: currentStatus.HudAnalysisMode == true ? "Analysis" : "Combat", buttonName : HUDMODE);
                matric.SetButtonProperties(CLIENT_ID, null, text: FormatStatusText(currentStatus), buttonName: STATUS);
                matric.SetButtonProperties(CLIENT_ID, null, text: FormatStatusLabel(currentStatus), buttonName: STATUS_LABEL);
            }
        }

        private void ClearButtonState() {
            Log.Instance.Info("Clearing Matric Button State");
            List<SetButtonsVisualStateArgs> buttons = new List<SetButtonsVisualStateArgs>();
            buttons.Add(new SetButtonsVisualStateArgs(null, "off", GEAR));
            buttons.Add(new SetButtonsVisualStateArgs(null, "off", SUPERCRUISE));
            buttons.Add(new SetButtonsVisualStateArgs(null, "off", FLIGHTASSIST));
            buttons.Add(new SetButtonsVisualStateArgs(null, "off", HARDPOINTS));
            buttons.Add(new SetButtonsVisualStateArgs(null, "off", LIGHTS));
            buttons.Add(new SetButtonsVisualStateArgs(null, "off", SCOOP));
            buttons.Add(new SetButtonsVisualStateArgs(null, "off", SILENT));
            buttons.Add(new SetButtonsVisualStateArgs(null, "off", HUDMODE));
            buttons.Add(new SetButtonsVisualStateArgs(null, "off", NVISION));
            buttons.Add(new SetButtonsVisualStateArgs(null, "off", FSDJUMP));
            buttons.Add(new SetButtonsVisualStateArgs(null, "off", DOCKED));
            buttons.Add(new SetButtonsVisualStateArgs(null, "off", LANDED));
            buttons.Add(new SetButtonsVisualStateArgs(null, "off", SHIELDS));
            buttons.Add(new SetButtonsVisualStateArgs(null, "off", WING));
            buttons.Add(new SetButtonsVisualStateArgs(null, "off", SCOOPINGFUEL));
            buttons.Add(new SetButtonsVisualStateArgs(null, "off", FSDMASSLOCK));
            buttons.Add(new SetButtonsVisualStateArgs(null, "off", FSDCHARGE));
            buttons.Add(new SetButtonsVisualStateArgs(null, "off", FSDCHARGE));
            buttons.Add(new SetButtonsVisualStateArgs(null, "off", LOWFUEL));
            buttons.Add(new SetButtonsVisualStateArgs(null, "off", OVERHEAT));
            buttons.Add(new SetButtonsVisualStateArgs(null, "off", INDANGER));
            buttons.Add(new SetButtonsVisualStateArgs(null, "off", INTERDICTION));
            buttons.Add(new SetButtonsVisualStateArgs(null, "off", MAINSHIP));
            buttons.Add(new SetButtonsVisualStateArgs(null, "off", FIGHTER));
            buttons.Add(new SetButtonsVisualStateArgs(null, "off", SRV));

            matric.SetButtonsVisualState(CLIENT_ID, buttons);
            matric.SetButtonProperties(CLIENT_ID, null, "Combat", buttonName: HUDMODE);
        }

        public void UpdateTarget(ShipTargetedData currentTarget) {

            if (currentTarget != null) {
                string targetTextColour = currentTarget.LegalStatus == "Wanted" ? ORANGE : WHITE;
                matric.SetButtonProperties(CLIENT_ID, null, text: FormatTargetText(currentTarget),textcolorOff: targetTextColour, buttonName: TARGET);
                matric.SetButtonProperties(CLIENT_ID, null, text: FormatTargetLabel(currentTarget), buttonName: TARGET_LABEL);
            }
        }

        private static string FormatTargetText(ShipTargetedData targetData) {

            String displayText = "";          
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

            String displayText = "";
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

            String displayText = "";
            if (statusData != null) {
                var statusTemplate = $"<table>";

                if (String.IsNullOrEmpty(statusData.BodyName) == true) {
                    statusTemplate = statusTemplate + $"<tr><td>Not available</td></tr>";
                } else {
                    statusTemplate = statusTemplate + $"<tr><td>{statusData.BodyName}</td></tr>";
                }
                if (String.IsNullOrEmpty(statusData.LegalState) == true) {
                    statusTemplate = statusTemplate + $"<tr><td>Unknown</td></tr>";
                } else {
                    statusTemplate = statusTemplate + $"<tr><td>{statusData.LegalState}</td></tr>";
                }
                
                statusTemplate = statusTemplate + $"<tr><td><br></td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>{statusData.Cargo.ToString()}</td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>{Math.Round((decimal)statusData.FuelMain, 2).ToString()}</td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>{Math.Round((decimal)statusData.FuelResovoir, 2).ToString()}</td></tr>";
                statusTemplate = statusTemplate + $"</table>";
                displayText = statusTemplate;
            }
            return displayText;
        }

        private static string FormatStatusLabel(StatusData statusData) {

            String displayText = "";
            if (statusData != null) {
                var statusTemplate = $"<table>";

                statusTemplate = statusTemplate + $"<tr><td>Closest body:</td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>Legal state:</td></tr>";
                statusTemplate = statusTemplate + $"<tr><td><br></td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>Cargo:</td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>Main fuel:</td></tr>";
                statusTemplate = statusTemplate + $"<tr><td>Fuel resovoir:</td></tr>";
                statusTemplate = statusTemplate + $"</table>";
                displayText = statusTemplate;
            }
            return displayText;
        }


    }
}
