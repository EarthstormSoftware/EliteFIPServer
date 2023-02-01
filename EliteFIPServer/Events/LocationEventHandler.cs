using EliteAPI.Abstractions.Events;
using EliteFIPProtocol;
using EliteFIPServer.Logging;
using System.Text.Json;

namespace EliteFIPServer {
    
    class LocationEventHandler {

        private IGameDataEvent Caller;

        public LocationEventHandler(IGameDataEvent caller) {
            Caller = caller;
        }

        public void HandleEvent(EliteAPI.Events.LocationEvent currentLocationData, EventContext context) {

            Log.Instance.Info("Handling Location Event");
            LocationData locationData = new LocationData();

            locationData.LastUpdate = DateTime.Now;
            locationData.SystemId = currentLocationData.SystemAddress;
            locationData.SystemName = currentLocationData.StarSystem;
            
            Caller.GameDataEvent(GameEventType.Location, locationData);
            
        }
    }
}
