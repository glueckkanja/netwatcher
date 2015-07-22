using System;

namespace NetWatcher
{
    [Flags]
    public enum Connectivity
    {
        Unknown = -1,
        Disconnected = 0,
        IPv6NoTraffic = 2,
        IPv4NoTraffic = 1,
        IPv4Subnet = 16,
        IPv4LocalNetwork = 32,
        IPv4Internet = 64,
        IPv6Subnet = 256,
        IPv6LocalNetwork = 512,
        IPv6Internet = 1024
    }
}