using NETWORKLIST;
using System;

namespace NetWatcher
{
    internal class NetworkEventArgs : EventArgs
    {
        public NetworkEventArgs(Guid networkID)
        {
            NetworkID = networkID;
        }

        public Guid NetworkID { get; private set; }
    }

    internal class NetworkEventConnectivityArgs : NetworkEventArgs
    {
        public NetworkEventConnectivityArgs(Guid networkID, NLM_CONNECTIVITY connectivity) : base(networkID)
        {
            Connectivity = connectivity;
        }

        public NLM_CONNECTIVITY Connectivity { get; private set; }
    }

    internal class NetworkEventPropertyArgs : NetworkEventArgs
    {
        public NetworkEventPropertyArgs(Guid networkID, NLM_NETWORK_PROPERTY_CHANGE property) : base(networkID)
        {
            Property = property;
        }

        public NLM_NETWORK_PROPERTY_CHANGE Property { get; private set; }
    }
}
