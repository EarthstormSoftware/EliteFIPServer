using EliteFIPProtocol;
using EliteFIPServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace EliteFIPServer
{
    class GameDataUpdateController 
    {

        private readonly IHubContext<GameDataUpdateHub> _hubContext;

        public GameDataUpdateController(IHubContext<GameDataUpdateHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public void SendStatusUpdate(StatusData statusData)
        {
            string statusJSON = JsonSerializer.Serialize(statusData);
            _hubContext.Clients.All.SendAsync("StatusData", statusJSON);
        }

        public void SendTargetUpdate(ShipTargetedData targetData)
        {
            string targetJSON = JsonSerializer.Serialize(targetData);
            _hubContext.Clients.All.SendAsync("TargetData", targetJSON);
        }

        public void SendLocationUpdate(LocationData locationData) {
            string locationJSON = JsonSerializer.Serialize(locationData);
            _hubContext.Clients.All.SendAsync("LocationData", locationJSON);
        }

        public void SendNavRouteUpdate(NavigationData navRouteData) {
            string locationJSON = JsonSerializer.Serialize(navRouteData);
            _hubContext.Clients.All.SendAsync("NavRouteData", locationJSON);
        }
    }
}
