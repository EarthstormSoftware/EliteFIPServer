using System;
using System.Text.Json;
using EliteFIPProtocol;
using EliteJournalReader.Events;
using EliteFIPServer.Logging;
using System.Collections.Generic;

namespace EliteFIPServer {

    class TargetEventHandler {

        private IGameDataEvent Caller;
        private Dictionary<string, string> ShipMap = new Dictionary<string, string>();

        public TargetEventHandler(IGameDataEvent caller) {
            Caller = caller;
            PopulateShipMap();
        }

        public void HandleEvent(object sender, ShipTargetedEvent.ShipTargetedEventArgs currentTargetData) {

            Log.Instance.Info("Handling Target Event: {targetevent}", currentTargetData.ToString());
            ShipTargetedData targetData = new ShipTargetedData();
            

            TimeSpan dataAge = DateTime.UtcNow.Subtract(currentTargetData.Timestamp);
            Log.Instance.Info("Target data age: {targetage}", dataAge.ToString());
            if (dataAge.TotalMinutes < 5) {             
                targetData.LastUpdate = currentTargetData.Timestamp;
                targetData.TargetLocked = currentTargetData.TargetLocked;
                if (targetData.TargetLocked == true) {
                    if (String.IsNullOrEmpty(currentTargetData.Ship_Localised)) {
                        string mappedShip = "";
                        if (ShipMap.TryGetValue(currentTargetData.Ship, out mappedShip)) {
                            targetData.Ship = mappedShip;
                        } else {
                            targetData.Ship = currentTargetData.Ship;
                        }

                    } else {
                        targetData.Ship = currentTargetData.Ship_Localised;
                    }
                    
                    targetData.ScanStage = currentTargetData.ScanStage;
                    if (targetData.ScanStage >= 1) {
                        targetData.PilotName = currentTargetData.PilotName_Localised;
                        targetData.PilotRank = currentTargetData.PilotRank.ToString();
                    }
                    if (targetData.ScanStage >= 2) {
                        targetData.ShieldHealth = currentTargetData.ShieldHealth;
                        targetData.HullHealth = currentTargetData.HullHealth;
                    }
                    if (targetData.ScanStage >= 3) {
                        targetData.Faction = currentTargetData.Faction;
                        targetData.LegalStatus = currentTargetData.LegalStatus;
                        targetData.SubSystem = currentTargetData.SubSystem;
                        targetData.SubSystemHealth = currentTargetData.SubSystemHealth;
                        targetData.Bounty = currentTargetData.Bounty;
                    }
                }
                Log.Instance.Info("Sending target data to worker {target}", targetData.ToString());
                Caller.GameDataEvent(GameEventType.Target, targetData);
            }
        }

        private void PopulateShipMap() {
            ShipMap.Add("adder", "Adder");
            ShipMap.Add("sidewinder", "Sidewinder");
            ShipMap.Add("anaconda", "Anaconda");
            ShipMap.Add("eagle", "Eagle");
            ShipMap.Add("hauler", "Hauler");
            ShipMap.Add("vulture", "Vulture");
            ShipMap.Add("python", "Python");
            ShipMap.Add("mamba", "Mamba");
        }

        
        public static FIPPacket CreateFIPPacket(ShipTargetedData targetData) {
            
            GameData gameData = new GameData();
            gameData.Type = GameEventType.Status.ToString();
            gameData.Data = JsonSerializer.Serialize(targetData);

            FIPPacket packet = new FIPPacket();
            packet.PacketType = PacketType.GameData.ToString();
            packet.Payload = JsonSerializer.Serialize(gameData);
            return packet;
        }
    }
}
