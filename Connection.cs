using System;
using System.Net.NetworkInformation;

namespace NetWatcher
{
    public class Connection
    {
        public Guid AdapterID { get; internal set; }
        public Guid ConnectionID { get; internal set; }
        public Connectivity Connectivity { get; internal set; }
        public DomainType DomainType { get; internal set; }
        public bool IsConnected { get; internal set; }
        public bool IsConnectedToInternet { get; internal set; }
        public Network Network { get; internal set; }
        public NetworkInterface Interface { get; set; }
    }
}