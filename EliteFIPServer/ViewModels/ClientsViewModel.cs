using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Matric.Integration;
using System.Collections.ObjectModel;

namespace EliteFIPServer.ViewModels
{
    /// <summary>
    /// ViewModel for Matric clients list
    /// </summary>
    public partial class ClientsViewModel : ObservableObject
    {
        private readonly CoreServer _coreServer;

        [ObservableProperty]
        private ObservableCollection<ClientInfo> clients = new();

        public ClientsViewModel(CoreServer coreServer)
        {
            _coreServer = coreServer;
        }

        [RelayCommand]
        public void RefreshClients()
        {
            var connectedClients = _coreServer.GetMatricApi().GetConnectedClients();
            Clients.Clear();
            foreach (var client in connectedClients)
            {
                Clients.Add(client);
            }
        }
    }
}
