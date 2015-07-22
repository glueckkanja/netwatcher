using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;
using NETWORKLIST;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;

namespace NetWatcher
{
    public class NetworkWatcher : IDisposable
    {
        private readonly Guid IID_INetworkEvents = typeof(INetworkEvents).GUID;

        private readonly Timer _buffer;
        private readonly List<Connection> _connections;

        private readonly INetworkListManager _manager;
        private readonly IConnectionPointContainer _container;
        private readonly IConnectionPoint _connectionPoint;
        private readonly int _cookie;

        private readonly NetworkEventSink _sink = new NetworkEventSink();

        public NetworkWatcher()
        {
            _buffer = new Timer(BufferCallback);

            _manager = new NetworkListManager();

            // init current connections
            _connections = GetCurrentConnections();

            // prep for networkevents
            _container = (IConnectionPointContainer)_manager;
            _container.FindConnectionPoint(ref IID_INetworkEvents, out _connectionPoint);

            // wire up sink object
            _sink.Added += NetworkAdded;
            _sink.ConnectivityChanged += NetworkConnectivityChanged;
            _sink.Deleted += NetworkDeleted;
            _sink.PropertyChanged += NetworkPropertyChanged;

            // enable raising events
            _connectionPoint.Advise(_sink, out _cookie);
        }

        ~NetworkWatcher()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // unmanaged stuff
            _connectionPoint.Unadvise(_cookie);

            ReleaseComObject(_connectionPoint);
            ReleaseComObject(_container);
            ReleaseComObject(_manager);

            if (disposing)
            {
                // managed stuff
                _buffer?.Dispose();
            }
        }

        public event EventHandler<NetworkWatcherEventArgs> ConnectionChanged;

        public List<Connection> Connections
        {
            get
            {
                List<Connection> copy;

                lock (_connections)
                {
                    copy = new List<Connection>(_connections);
                }

                return copy;
            }
        }

        public TimeSpan EventBufferDuration { get; set; } = TimeSpan.FromSeconds(5);

        private void NetworkAdded(object sender, NetworkEventArgs e)
        {
            var connections = GetCurrentConnectionsByNetwork(e.NetworkID);

            if (connections == null)
            {
                return;
            }

            lock (_connections)
            {
                RemoveByNetworkID(e.NetworkID);
                _connections.AddRange(connections);
            }

            Invoked();
        }

        private void NetworkConnectivityChanged(object sender, NetworkEventConnectivityArgs e)
        {
            var connections = GetCurrentConnectionsByNetwork(e.NetworkID);

            if (connections == null)
            {
                return;
            }

            lock (_connections)
            {
                RemoveByNetworkID(e.NetworkID);
                _connections.AddRange(connections);
            }

            Invoked();
        }

        private void NetworkDeleted(object sender, NetworkEventArgs e)
        {
            lock (_connections)
            {
                RemoveByNetworkID(e.NetworkID);
            }

            Invoked();
        }

        private void NetworkPropertyChanged(object sender, NetworkEventPropertyArgs e)
        {
        }

        private void RemoveByNetworkID(Guid networkID)
        {
            _connections.RemoveAll(x => x.Network.NetworkID == networkID);
        }

        private void Invoked()
        {
            _buffer.Change(EventBufferDuration, TimeSpan.Zero);
        }

        private void BufferCallback(object state)
        {
            var handler = ConnectionChanged;
            handler?.Invoke(this, new NetworkWatcherEventArgs(Connections));
        }

        private List<Connection> GetCurrentConnections()
        {
            IEnumNetworkConnections enumConnections = null;
            List<INetworkConnection> connections = new List<INetworkConnection>();

            try
            {
                enumConnections = _manager.GetNetworkConnections();
                connections.AddRange(enumConnections.Cast<INetworkConnection>());

                return connections.Select(ConvertConnection).ToList();
            }
            catch (COMException e) when ((uint)e.ErrorCode == 0x8000FFFF)
            {
                return null;
            }
            finally
            {
                foreach (var connection in connections) ReleaseComObject(connection);
                ReleaseComObject(enumConnections);
            }
        }

        private List<Connection> GetCurrentConnectionsByNetwork(Guid networkID)
        {
            INetwork network = null;
            IEnumNetworkConnections enumConnections = null;
            List<INetworkConnection> connections = new List<INetworkConnection>();

            try
            {
                var results = new List<Connection>();

                network = _manager.GetNetwork(networkID);
                enumConnections = network.GetNetworkConnections();
                connections.AddRange(enumConnections.Cast<INetworkConnection>());

                return connections.Select(ConvertConnection).ToList();
            }
            catch (COMException e) when ((uint)e.ErrorCode == 0x8000FFFF)
            {
                return null;
            }
            finally
            {
                foreach (var connection in connections) ReleaseComObject(connection);
                ReleaseComObject(enumConnections);
                ReleaseComObject(network);
            }
        }

        private Connection ConvertConnection(INetworkConnection connection)
        {
            INetwork network = null;

            try
            {
                network = connection.GetNetwork();

                uint crLo, crHi, coLo, coHi;
                network.GetTimeCreatedAndConnected(out crLo, out crHi, out coLo, out coHi);

                return new Connection
                {
                    AdapterID = connection.GetAdapterId(),

                    IsConnected = connection.IsConnected,
                    IsConnectedToInternet = connection.IsConnectedToInternet,
                    ConnectionID = connection.GetConnectionId(),
                    Connectivity = (Connectivity)connection.GetConnectivity(),
                    DomainType = (DomainType)connection.GetDomainType(),

                    Network = ConvertNetwork(network)
                };
            }
            finally
            {
                ReleaseComObject(network);
            }
        }

        private Network ConvertNetwork(INetwork network)
        {
            uint crLo, crHi, coLo, coHi;
            network.GetTimeCreatedAndConnected(out crLo, out crHi, out coLo, out coHi);

            return new Network
            {
                Category = (NetworkCategory)network.GetCategory(),
                Description = network.GetDescription(),
                Name = network.GetName(),
                NetworkID = network.GetNetworkId(),
                Created = DateTimeOffset.FromFileTime((long)(((ulong)crHi) << 32 | crLo)),
                Connected = DateTimeOffset.FromFileTime((long)(((ulong)coHi) << 32 | coLo)),
            };
        }

        private static void ReleaseComObject(object obj)
        {
            if (obj != null && Marshal.IsComObject(obj))
                Marshal.FinalReleaseComObject(obj);
        }
    }
}