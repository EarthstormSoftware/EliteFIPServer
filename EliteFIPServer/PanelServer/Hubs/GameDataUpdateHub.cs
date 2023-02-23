using Microsoft.AspNetCore.SignalR;

namespace EliteFIPServer
{
    public class GameDataUpdateHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            ClientConnect.RequestDataUpdate();
            await base.OnConnectedAsync();
        }
    }
}
