using EliteAPI.Abstractions.Events;
using EliteFIPProtocol;
using EliteFIPServer.Logging;

namespace EliteFIPServer {

    class NavRouteEventHandler {

        private IGameDataEvent Caller;

        public NavRouteEventHandler(IGameDataEvent caller) {
            Caller = caller;
        }

        public void HandleEvent(EliteAPI.Events.Status.NavRoute.NavRouteEvent currentNavRouteData, EventContext context) {

            Log.Instance.Info("Handling NavRoute Event");
            NavigationData navigationData = new NavigationData();

            navigationData.LastUpdate = DateTime.Now;
            if ((currentNavRouteData.Stops != null) && (currentNavRouteData.Stops.Count() != 0)) {
                navigationData.NavRouteActive = true;
                foreach (EliteAPI.Events.Status.NavRoute.NavRouteStop navRouteStop in currentNavRouteData.Stops) {
                    NavigationData.NavRouteStop navStop = new NavigationData.NavRouteStop();
                    navStop.SystemId = navRouteStop.Address;
                    navStop.SystemName = navRouteStop.System;
                    navStop.Class = navRouteStop.Class;
                    navigationData.Stops.Add(navStop);
                }
            } else {
                navigationData.NavRouteActive = false;
            }
            Caller.GameDataEvent(GameEventType.Navigation, navigationData);
        }
    }
}
