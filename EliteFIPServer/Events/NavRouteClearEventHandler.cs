using EliteAPI.Abstractions.Events;
using EliteFIPProtocol;
using EliteFIPServer.Logging;

namespace EliteFIPServer {
    
    class NavRouteClearEventHandler {

        private IGameDataEvent Caller;

        public NavRouteClearEventHandler(IGameDataEvent caller) {
            Caller = caller;
        }

        public void HandleEvent(EliteAPI.Events.Status.NavRoute.NavRouteClearEvent navRouteClear, EventContext context) {

            Log.Instance.Info("Handling NavRouteClear Event");
            NavigationData navigationData = new NavigationData();

            navigationData.LastUpdate = DateTime.Now;            

            Log.Instance.Info("Sending NavigationData to worker");
            Caller.GameDataEvent(GameEventType.Location, navigationData);
            
        }
    }
}
