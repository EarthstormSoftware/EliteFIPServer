﻿using EliteAPI.Abstractions.Events;
using EliteFIPProtocol;
using EliteFIPServer.Logging;
using System.Text.Json;

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
            if (currentNavRouteData.Stops != null) {
                foreach (EliteAPI.Events.Status.NavRoute.NavRouteStop navRouteStop in currentNavRouteData.Stops) {
                    NavigationData.NavRouteStop navStop = new NavigationData.NavRouteStop();
                    navStop.SystemId = navRouteStop.Address;
                    navStop.SystemName = navRouteStop.System;
                    navStop.Class = navRouteStop.Class;
                    navigationData.Stops.Add(navStop);
                }
            }
            Log.Instance.Info("Sending NavigationData to worker");
            Caller.GameDataEvent(GameEventType.Location, navigationData);
            
        }
    }
}