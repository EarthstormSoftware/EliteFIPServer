using Microsoft.AspNetCore.SignalR;

namespace EliteFIPServer
{
    public class GameDataUpdateHub : Hub
    {
        public static event EventHandler<string> ClientConnected;
        public static event EventHandler<string> ClientDisconnected;

        public override async Task OnConnectedAsync()
        {
            ClientConnected?.Invoke(this, Context.ConnectionId);
            ClientConnect.RequestDataUpdate();
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            ClientDisconnected?.Invoke(this, Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
