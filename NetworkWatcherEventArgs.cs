using System;
using System.Collections.Generic;

namespace NetWatcher
{
    public class NetworkWatcherEventArgs : EventArgs
    {
        public NetworkWatcherEventArgs(ICollection<Connection> connections)
        {
            Connections = connections;
        }

        public ICollection<Connection> Connections { get; private set; }
    }
}