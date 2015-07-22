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
            var handler = Added;
            handler?.Invoke(this, new NetworkEventArgs(networkId));
        }

        public void NetworkConnectivityChanged(Guid networkId, NLM_CONNECTIVITY newConnectivity)
        {
            var handler = ConnectivityChanged;
            handler?.Invoke(this, new NetworkEventConnectivityArgs(networkId, newConnectivity));
        }

        public void NetworkDeleted(Guid networkId)
        {
            var handler = Deleted;
            handler?.Invoke(this, new NetworkEventArgs(networkId));
        }

        public void NetworkPropertyChanged(Guid networkId, NLM_NETWORK_PROPERTY_CHANGE flags)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new NetworkEventPropertyArgs(networkId, flags));
        }
    }
}
