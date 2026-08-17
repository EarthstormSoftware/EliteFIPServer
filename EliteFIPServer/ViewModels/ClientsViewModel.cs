using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EliteFIPServer.Logging;
using Matric.Integration;
using System;
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
            try
            {
                if (_coreServer == null)
                {
                    Clients.Clear();
                    return;
                }

                var matricApi = _coreServer.GetMatricApi();
                if (matricApi == null)
                {
                    Clients.Clear();
                    return;
                }

                var connectedClients = matricApi.GetConnectedClients();
                if (connectedClients == null)
                {
                    Clients.Clear();
                    return;
                }

                Clients.Clear();
                foreach (var client in connectedClients)
                {
                    Clients.Add(client);
                }
            }
            catch (Exception ex)
            {
                Clients.Clear();
                Logging.Log.Instance.Error("Error refreshing Matric clients: {error}", ex.ToString());
            }
        }
    }
}
