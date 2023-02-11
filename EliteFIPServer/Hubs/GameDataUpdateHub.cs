using Microsoft.AspNetCore.SignalR;

namespace EliteFIPServer.Hubs {
    public class GameDataUpdateHub : Hub {
        public override async Task OnConnectedAsync() {
            ClientConnect.requestDataUpdate();
            await base.OnConnectedAsync();
        }
    }
}
