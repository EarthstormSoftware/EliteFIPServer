using System;
using System.Text.Json;
using EliteFIPProtocol;
using EliteJournalReader.Events;
using EliteFIPServer.Logging;

namespace EliteFIPServer {

    class StatusEventHandler {

        private IGameDataEvent Caller;
        
        public StatusEventHandler(IGameDataEvent caller) {
            Caller = caller;
        }

        public void HandleEvent(object sender, StatusFileEvent currentStatusData) {

            Log.Instance.Info("Handling Status Event: {statusevent}", currentStatusData.ToString());
            StatusData statusData = new StatusData();

            if (currentStatusData != null) {

                
                statusData.LastUpdate = DateTime.Now;

                StatusFlags curState = (StatusFlags)currentStatusData.Flags;
                if (curState.HasFlag(StatusFlags.LandingGearDown)) { statusData.LandingGearDown = true; }
                if (curState.HasFlag(StatusFlags.HardpointsDeployed)) { statusData.HardpointsDeployed = true; }
                if (curState.HasFlag(StatusFlags.LightsOn)) { statusData.LightsOn = true; }
                if (curState.HasFlag(StatusFlags.CargoScoopDeployed)) { statusData.CargoScoopDeployed = true; }
                if (curState.HasFlag(StatusFlags.SilentRunning)) { statusData.SilentRunning = true; }
                if (curState.HasFlag(StatusFlags.NightVision)) { statusData.NightVision = true; }
                if (curState.HasFlag(StatusFlags.HudInAnalysisMode)) { statusData.HudAnalysisMode = true; }
                if (curState.HasFlag(StatusFlags.InMainShip)) { statusData.InMainShip = true; }
                if (curState.HasFlag(StatusFlags.InFighter)) { statusData.InFighter = true; }
                if (curState.HasFlag(StatusFlags.ShieldsUp)) { statusData.ShieldsUp = true; }

                if (currentStatusData.Flags != 0) {
                    statusData.SystemPips = currentStatusData.Pips.System;
                    statusData.EnginePips = currentStatusData.Pips.Engine;
                    statusData.WeaponPips = currentStatusData.Pips.Weapons;

                    statusData.FireGroup = currentStatusData.Firegroup;
                    //GuiFocus = currentGameState.GuiFocus;
                    statusData.FuelMain = currentStatusData.Fuel.FuelMain;
                    statusData.FuelResovoir = currentStatusData.Fuel.FuelReservoir;
                    statusData.Cargo = currentStatusData.Cargo;
                    statusData.LegalState = currentStatusData.LegalState;
                }
                Log.Instance.Info("Sending Status to worker {status}", statusData.ToString());
                Caller.GameDataEvent(GameEventType.Status, statusData);
            }            
        }

        public static FIPPacket CreateFIPPacket(StatusData statusData) {
            
            GameData gameData = new GameData();
            gameData.Type = GameEventType.Status.ToString();
            gameData.Data = JsonSerializer.Serialize(statusData);

            FIPPacket packet = new FIPPacket();
            packet.PacketType = PacketType.GameData.ToString();
            packet.Payload = JsonSerializer.Serialize(gameData);
            return packet;
        }
    }
}
