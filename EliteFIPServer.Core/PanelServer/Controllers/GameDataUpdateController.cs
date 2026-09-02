using EliteFIPProtocol;
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

        public async Task SendStatusUpdate(StatusData statusData)
        {
            string statusJSON = JsonSerializer.Serialize(statusData);
            await _hubContext.Clients.All.SendAsync("StatusData", statusJSON);
        }

        public async Task SendTargetUpdate(ShipTargetedData targetData)
        {
            string targetJSON = JsonSerializer.Serialize(targetData);
            await _hubContext.Clients.All.SendAsync("TargetData", targetJSON);
        }

        public async Task SendLocationUpdate(LocationData locationData)
        {
            string locationJSON = JsonSerializer.Serialize(locationData);
            await _hubContext.Clients.All.SendAsync("LocationData", locationJSON);
        }

        public async Task SendNavRouteUpdate(NavigationData navRouteData)
        {
            string navRouteJSON = JsonSerializer.Serialize(navRouteData);
            await _hubContext.Clients.All.SendAsync("NavRouteData", navRouteJSON);
        }

        public async Task SendPreviousNavRoute(NavigationData navRouteData) {
            string navRouteJSON = JsonSerializer.Serialize(navRouteData);
            await _hubContext.Clients.All.SendAsync("PreviousNavRoute", navRouteJSON);
        }

        public async Task SendJumpUpdate(JumpData jumpData) {
            string jumpJSON = JsonSerializer.Serialize(jumpData);
            await _hubContext.Clients.All.SendAsync("JumpData", jumpJSON);
        }
    }
}
