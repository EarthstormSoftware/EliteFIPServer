using EliteAPI.Abstractions.Events;
using EliteFIPProtocol;
using EliteFIPServer.Logging;
using System.Text.Json;

namespace EliteFIPServer {

    class TargetEventHandler {

        private IGameDataEvent Caller;        

        public TargetEventHandler(IGameDataEvent caller) {
            Caller = caller;
        }

        public void HandleEvent(EliteAPI.Events.ShipTargetedEvent currentTargetData, EventContext context) {

            Log.Instance.Info("Handling ShipTargetedEvent");
            Log.Instance.Info("Targetlock: {istargetlocked}, Scanstage: {scanstage}", currentTargetData.IsTargetLocked.ToString(), currentTargetData.ScanStage.ToString());
            ShipTargetedData targetData = new ShipTargetedData();

            TimeSpan dataAge = DateTime.UtcNow.Subtract(currentTargetData.Timestamp);
            Log.Instance.Info("Target data age: {targetage}", dataAge.ToString());
            if (dataAge.TotalMinutes < 5) {
                targetData.LastUpdate = currentTargetData.Timestamp;
                targetData.TargetLocked = currentTargetData.IsTargetLocked;
                if (targetData.TargetLocked == true) {
                    
                    // Ensure ship name starts with a Uppercase letter to look nice for nonlocalised ships
                    targetData.Ship = char.ToUpper(currentTargetData.Ship.ToString()[0]) + currentTargetData.Ship.ToString().Substring(1);
                    
                    // In ELiteAPI Scanstage is a long, in ELiteFIPProtocl it's an int.
                    targetData.ScanStage = (int)currentTargetData.ScanStage;
                    if (targetData.ScanStage >= 1) {
                        if (String.IsNullOrEmpty(currentTargetData.PilotName.ToString())) {
                            targetData.PilotName = currentTargetData.PilotName.Symbol;
                        } else {
                            targetData.PilotName = currentTargetData.PilotName.ToString();
                        }                        
                        targetData.PilotRank = currentTargetData.PilotRank;
                    }
                    if (targetData.ScanStage >= 2) {
                        targetData.ShieldHealth = currentTargetData.ShieldHealth;
                        targetData.HullHealth = currentTargetData.HullHealth;
                    }
                    if (targetData.ScanStage >= 3) {
                        targetData.Faction = currentTargetData.Faction;
                        targetData.LegalStatus = currentTargetData.LegalStatus;
                        targetData.SubSystemHealth = currentTargetData.SubsystemHealth;
                        targetData.Bounty = currentTargetData.Bounty;
                        if (String.IsNullOrEmpty(currentTargetData.Subsystem.ToString())) {
                            targetData.SubSystem = currentTargetData.Subsystem.Symbol;
                        } else {
                            targetData.SubSystem = currentTargetData.Subsystem.ToString();
                        }
                    }                                    
                }
                Log.Instance.Info("Sending target data to worker");
                Caller.GameDataEvent(GameEventType.Target, targetData);
            }
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
