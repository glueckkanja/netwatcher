using NETWORKLIST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;

namespace NetWatcher
{
    public class NetworkWatcher : IDisposable
    {
        private readonly Timer _buffer;

        private readonly AsyncLock _connectionsAsyncLock = new AsyncLock();
        private readonly List<Connection> _connectionsAsync = new List<Connection>();

        private INetworkListManager _manager;
        private IConnectionPointContainer _container;
        private IConnectionPoint _connectionPoint;
        private int _cookie;

        private readonly NetworkEventSink _sink = new NetworkEventSink();

        public NetworkWatcher()
        {
            _buffer = new Timer(BufferCallbackAsync);

            // wire up sink object
            _sink.Added += NetworkAddedAsync;
            _sink.ConnectivityChanged += NetworkConnectivityChangedAsync;
            _sink.Deleted += NetworkDeletedAsync;
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

            if (disposing)
            {
                // managed stuff
                _buffer?.Dispose();
            }
        }

        public async Task InitAsync()
        {
            using (await _connectionsAsyncLock.LockAsync())
            {
                // init manager - takes a few ms
                _manager = new NetworkListManager();

                // init current connections - takes a few ms
                _connectionsAsync.AddRange(GetCurrentConnections());
                Connections = new List<Connection>(_connectionsAsync);

                // get .net interfaces - takes a few ms
                Interfaces = new List<NetworkInterface>(NetworkInterface.GetAllNetworkInterfaces());

                foreach (var connection in _connectionsAsync)
                {
                    connection.Interface = FindInterface(connection, Interfaces);
                }
            }

            // prep for networkevents
            var iid = typeof(INetworkEvents).GUID;
            _container = (IConnectionPointContainer)_manager;
            _container.FindConnectionPoint(ref iid, out _connectionPoint);

            // enable raising events
            _connectionPoint.Advise(_sink, out _cookie);
        }

        public event EventHandler ConnectionsChanged;

        public List<Connection> Connections { get; private set; }
        public List<NetworkInterface> Interfaces { get; private set; }

        public TimeSpan EventBufferDuration { get; set; } = TimeSpan.FromSeconds(5);

        private async void NetworkAddedAsync(object sender, NetworkEventArgs e)
        {
            var connections = GetCurrentConnectionsByNetwork(e.NetworkID);

            if (connections == null)
                return;

            using (await _connectionsAsyncLock.LockAsync())
            {
                _connectionsAsync.RemoveAll(x => x.Network.NetworkID == e.NetworkID);
                _connectionsAsync.AddRange(connections);
            }

            RestartBufferTimer();
        }

        private async void NetworkConnectivityChangedAsync(object sender, NetworkEventConnectivityArgs e)
        {
            var connections = GetCurrentConnectionsByNetwork(e.NetworkID);

            if (connections == null)
                return;

            using (await _connectionsAsyncLock.LockAsync())
            {
                _connectionsAsync.RemoveAll(x => x.Network.NetworkID == e.NetworkID);
                _connectionsAsync.AddRange(connections);
            }

            RestartBufferTimer();
        }

        private async void NetworkDeletedAsync(object sender, NetworkEventArgs e)
        {
            using (await _connectionsAsyncLock.LockAsync())
            {
                _connectionsAsync.RemoveAll(x => x.Network.NetworkID == e.NetworkID);
            }

            RestartBufferTimer();
        }

        private void RestartBufferTimer()
        {
            _buffer.Change(EventBufferDuration, TimeSpan.Zero);
        }

        private async void BufferCallbackAsync(object state)
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            using (await _connectionsAsyncLock.LockAsync())
            {
                foreach (var connection in _connectionsAsync)
                {
                    connection.Interface = FindInterface(connection, Interfaces);
                }

                Connections = new List<Connection>(_connectionsAsync);
                Interfaces = new List<NetworkInterface>(interfaces);
            }

            ConnectionsChanged?.Invoke(this, EventArgs.Empty);
        }

        private IEnumerable<Connection> GetCurrentConnections()
        {
            try
            {
                return _manager.GetNetworkConnections()
                    .Cast<INetworkConnection>()
                    .Select(ConvertConnection);
            }
            catch (COMException e) when ((uint)e.ErrorCode == 0x8000FFFF)
            {
                return null;
            }
        }

        private IEnumerable<Connection> GetCurrentConnectionsByNetwork(Guid networkID)
        {
            try
            {
                return _manager.GetNetwork(networkID).GetNetworkConnections()
                    .Cast<INetworkConnection>()
                    .Select(ConvertConnection);
            }
            catch (COMException e) when ((uint)e.ErrorCode == 0x8000FFFF)
            {
                return null;
            }
        }

        private Connection ConvertConnection(INetworkConnection connection)
        {
            var network = connection.GetNetwork();

            uint crLo, crHi, coLo, coHi;
            network.GetTimeCreatedAndConnected(out crLo, out crHi, out coLo, out coHi);

            var adapterID = connection.GetAdapterId();

            return new Connection
            {
                AdapterID = adapterID,

                IsConnected = connection.IsConnected,
                IsConnectedToInternet = connection.IsConnectedToInternet,
                ConnectionID = connection.GetConnectionId(),
                Connectivity = (Connectivity)connection.GetConnectivity(),
                DomainType = (DomainType)connection.GetDomainType(),

                Network = ConvertNetwork(network),
            };
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

        private NetworkInterface FindInterface(Connection connection, IEnumerable<NetworkInterface> interfaces)
        {
            return interfaces.FirstOrDefault(x => string.Equals(x.Id, connection.AdapterID.ToString("b"), StringComparison.OrdinalIgnoreCase));
        }
    }
}