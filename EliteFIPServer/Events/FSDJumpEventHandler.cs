using EliteAPI.Abstractions.Events;
using EliteFIPProtocol;
using EliteFIPServer.Logging;
using System.Text.Json;

namespace EliteFIPServer {

    class FSDJumpEventHandler {

        private IGameDataEvent Caller;

        public FSDJumpEventHandler(IGameDataEvent caller) {
            Caller = caller;
        }

        public void HandleEvent(EliteAPI.Events.FsdJumpEvent fsdJumpdataData, EventContext context) {

            Log.Instance.Info("Handling FsdJumpEvent Event");
            LocationData locationData = new LocationData();

            locationData.LastUpdate = DateTime.Now;
            locationData.SystemId = fsdJumpdataData.SystemAddress;
            locationData.SystemName = fsdJumpdataData.StarSystem;

            Log.Instance.Info("Sending Location to worker");
            Caller.GameDataEvent(GameEventType.Location, locationData);

        }

        public static FIPPacket CreateFIPPacket(LocationData locationData) {

            GameData gameData = new GameData();
            gameData.Type = GameEventType.Location.ToString();
            gameData.Data = JsonSerializer.Serialize(locationData);

            FIPPacket packet = new FIPPacket();
            packet.PacketType = PacketType.GameData.ToString();
            packet.Payload = JsonSerializer.Serialize(gameData);
            return packet;
        }
    }
}
