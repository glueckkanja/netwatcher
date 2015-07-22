using System;

namespace NetWatcher
{
    public class Network
    {
        public NetworkCategory Category { get; internal set; }
        public DateTimeOffset Connected { get; internal set; }
        public DateTimeOffset Created { get; internal set; }
        public string Description { get; internal set; }
        public string Name { get; internal set; }
        public Guid NetworkID { get; internal set; }
    }
}