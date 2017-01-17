using NETWORKLIST;
using System;

namespace NetWatcher
{
    internal class NetworkEventSink : INetworkEvents
    {
        public event EventHandler<NetworkEventArgs> Added;
        public event EventHandler<NetworkEventConnectivityArgs> ConnectivityChanged;
        public event EventHandler<NetworkEventArgs> Deleted;
        public event EventHandler<NetworkEventPropertyArgs> PropertyChanged;

        public void NetworkAdded(Guid networkId)
        {
            Added?.Invoke(this, new NetworkEventArgs(networkId));
        }

        public void NetworkConnectivityChanged(Guid networkId, NLM_CONNECTIVITY newConnectivity)
        {
            ConnectivityChanged?.Invoke(this, new NetworkEventConnectivityArgs(networkId, newConnectivity));
        }

        public void NetworkDeleted(Guid networkId)
        {
            Deleted?.Invoke(this, new NetworkEventArgs(networkId));
        }

        public void NetworkPropertyChanged(Guid networkId, NLM_NETWORK_PROPERTY_CHANGE flags)
        {
            PropertyChanged?.Invoke(this, new NetworkEventPropertyArgs(networkId, flags));
        }
    }
}
