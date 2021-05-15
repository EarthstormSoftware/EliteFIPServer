using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Matric.Integration;
using EliteFIPServer.Logging;
using EliteFIPProtocol;

namespace EliteFIPServer {
    public class MatricIntegration {
        
        public static string DECK_EDVFIP = "ea9601d6-e270-45e6-89f6-c0dc7f018c15";
        
        public static string PAGE_FLIGHT = "94c77f00-54be-46c4-a40e-5c7b2a65492f";
        public static string PAGE_COMBAT = "e16dda5c-5bb3-4187-b48f-43e52fab1088";

        public static string GEAR = "btnGear";
        public static string HARDPOINTS = "btnHardpoints";
        public static string SCOOP = "btnScoop";
        public static string LIGHTS = "btnLights";
        public static string NVISION = "btnNVision";
        public static string SILENT = "btnSilent";
        public static string HUDMODE = "btnHudMode";
        public static string TARGET = "txtTarget";
        public static string TARGET_LABEL = "txtTargetLabel";

        private string WHITE = "#FFFFFF";
        private string ORANGE = "#EC8908";
        
        public static List<ClientInfo> ConnectedClients = new List<ClientInfo>();

        private string AppName = "Elite FIP Server";
        private static string CLIENT_ID;
        private static Matric.Integration.Matric matric;

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

        public List<ClientInfo> GetConnectClients() {            
            return ConnectedClients;
        }

        public void SetClientId(string clientId) {
            CLIENT_ID = clientId;
        }

        public void UpdateStatus(StatusData currentStatus) {
            
            if (currentStatus != null) {
                List<SetButtonsVisualStateArgs> buttons = new List<SetButtonsVisualStateArgs>();
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.LandingGearDown == true ? "on" : "off" , GEAR)) ;
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.HardpointsDeployed == true ? "on" : "off", HARDPOINTS));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.CargoScoopDeployed == true ? "on" : "off", SCOOP));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.LightsOn == true ? "on" : "off", LIGHTS));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.NightVision == true ? "on" : "off", NVISION));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.SilentRunning == true ? "on" : "off", SILENT));
                buttons.Add(new SetButtonsVisualStateArgs(null, currentStatus.HudAnalysisMode == true ? "on" : "off", HUDMODE));
                
                matric.SetButtonsVisualState(CLIENT_ID, buttons);
                matric.SetButtonProperties(CLIENT_ID, null, text: currentStatus.HudAnalysisMode == true ? "Analysis\r\nMode" : "Combat\r\nMode", buttonName : HUDMODE);
            }
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

    }
}
