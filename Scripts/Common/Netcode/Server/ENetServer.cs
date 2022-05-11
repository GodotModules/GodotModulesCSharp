using ENet;
using System.Threading;

namespace GodotModules.Netcode.Server
{
    public abstract class ENetServer : IDisposable
    {
        private static readonly Dictionary<ClientPacketOpcode, APacketClient> HandlePacket = ReflectionUtils.LoadInstances<ClientPacketOpcode, APacketClient>("CPacket");

        public bool HasSomeoneConnected { get => Interlocked.Read(ref _someoneConnected) == 1; }
        public bool IsRunning { get => Interlocked.Read(ref _running) == 1; }
        public ConcurrentQueue<ENetServerCmd> ENetCmds = new ConcurrentQueue<ENetServerCmd>();

        protected Dictionary<uint, Peer> Peers = new Dictionary<uint, Peer>();
        protected CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        protected bool _queueRestart { get; set; }

        private long _someoneConnected = 0;
        private long _running = 0;
        private ConcurrentQueue<ServerPacket> _outgoing = new ConcurrentQueue<ServerPacket>();

        public async Task StartAsync(ushort port, int maxClients)
        {
            try
            {
                if (IsRunning)
                {
                    GM.Log("Server is running already");
                    return;
                }

                _running = 1;
                CancellationTokenSource = new CancellationTokenSource();

                await Task.Run(() => ENetThreadWorker(port, maxClients), CancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                GM.LogErr(e, "Server");
            }
        }

        public void KickAll(DisconnectOpcode opcode)
        {
            Peers.Values.ForEach(peer => peer.DisconnectNow((uint)opcode));
            Peers.Clear();
        }

        public void Kick(uint id, DisconnectOpcode opcode)
        {
            Peers[id].DisconnectNow((uint)opcode);
            Peers.Remove(id);
        }

        public void Stop() => ENetCmds.Enqueue(new ENetServerCmd(ENetServerOpcode.Stop));
        public async Task StopAsync()
        {
            Stop();

            while (IsRunning)
                await Task.Delay(1);
        }
        public void Restart() => ENetCmds.Enqueue(new ENetServerCmd(ENetServerOpcode.Restart));
        public void Send(ServerPacketOpcode opcode, params Peer[] peers) => Send(opcode, null, PacketFlags.Reliable, peers);
        public void Send(ServerPacketOpcode opcode, APacket data, params Peer[] peers) => Send(opcode, data, PacketFlags.Reliable, peers);
        public void Send(ServerPacketOpcode opcode, PacketFlags flags = PacketFlags.Reliable, params Peer[] peers) => Send(opcode, null, flags, peers);
        public void Send(ServerPacketOpcode opcode, APacket data, PacketFlags flags = PacketFlags.Reliable, params Peer[] peers) => _outgoing.Enqueue(new ServerPacket((byte)opcode, flags, data, peers));

        protected Peer[] GetOtherPeers(uint id)
        {
            var otherPeers = new Dictionary<uint, Peer>(Peers);
            otherPeers.Remove(id);
            return otherPeers.Values.ToArray();
        }

        protected virtual void Started(ushort port, int maxClients) { }
        protected virtual void Connect(Event netEvent) { }
        protected virtual void Received(ClientPacketOpcode opcode) { }
        protected virtual void Disconnect(Event netEvent) { }
        protected virtual void Timeout(Event netEvent) { }
        protected virtual void Leave(Event netEvent) { }
        protected virtual void Stopped() { }
        protected virtual void ServerCmds() { }

        private Task ENetThreadWorker(ushort port, int maxClients)
        {
            using var server = new Host();
            Address address = new Address();
            address.Port = port;

            try
            {
                server.Create(address, maxClients);
            }
            catch (InvalidOperationException e)
            {
                var message = $"A server is running on port {port} already! {e.Message}";
                GM.LogWarning(message);
                Cleanup();
                return Task.FromResult(1);
            }

            Started(port, maxClients);

            while (!CancellationTokenSource.IsCancellationRequested)
            {
                var polled = false;

                // ENet Cmds
                ServerCmds();

                // Outgoing
                while (_outgoing.TryDequeue(out ServerPacket packet))
                    packet.Peers.ForEach(peer => Send(packet, peer));

                while (!polled)
                {
                    if (server.CheckEvents(out Event netEvent) <= 0)
                    {
                        if (server.Service(15, out netEvent) <= 0)
                            break;

                        polled = true;
                    }

                    var peer = netEvent.Peer;
                    var eventType = netEvent.Type;

                    switch (eventType)
                    {
                        case EventType.Receive:
                            var packet = netEvent.Packet;
                            if (packet.Length > GamePacket.MaxSize)
                            {
                                GM.LogWarning($"Tried to read packet from client of size {packet.Length} when max packet size is {GamePacket.MaxSize}");
                                packet.Dispose();
                                continue;
                            }

                            var packetReader = new PacketReader(packet);
                            var opcode = (ClientPacketOpcode)packetReader.ReadByte();

                            Received(opcode);

                            if (!HandlePacket.ContainsKey(opcode))
                            {
                                GM.LogWarning($"[Server]: Received malformed opcode: {opcode} (Ignoring)");
                                break;
                            }

                            var handlePacket = HandlePacket[opcode];
                            try
                            {
                                handlePacket.Read(packetReader);
                            }
                            catch (System.IO.EndOfStreamException e)
                            {
                                GM.LogWarning($"[Server]: Received malformed opcode: {opcode} {e.Message} (Ignoring)");
                                break;
                            }
                            handlePacket.Handle(netEvent.Peer);

                            packetReader.Dispose();
                            break;

                        case EventType.Connect:
                            _someoneConnected = 1;
                            Peers[netEvent.Peer.ID] = netEvent.Peer;
                            Connect(netEvent);
                            break;

                        case EventType.Disconnect:
                            Peers.Remove(netEvent.Peer.ID);
                            Disconnect(netEvent);
                            Leave(netEvent);
                            break;

                        case EventType.Timeout:
                            Peers.Remove(netEvent.Peer.ID);
                            Timeout(netEvent);
                            Leave(netEvent);
                            break;
                    }
                }
            }

            server.Flush();
            Cleanup();

            if (_queueRestart)
            {
                _queueRestart = false;
                GM.Net.StartServer(port, maxClients);
            }

            return Task.FromResult(1);
        }

        private void Send(ServerPacket gamePacket, Peer peer)
        {
            var packet = default(Packet);
            packet.Create(gamePacket.Data, gamePacket.PacketFlags);
            byte channelID = 0;
            peer.Send(channelID, ref packet);
        }

        private void Cleanup()
        {
            _running = 0;
            Stopped();
        }

        public void Dispose()
        {
            CancellationTokenSource.Dispose();
        }
    }

    public struct ENetServerCmd
    {
        public ENetServerOpcode Opcode { get; set; }
        public object Data { get; set; }

        public ENetServerCmd(ENetServerOpcode opcode, object data = null)
        {
            Opcode = opcode;
            Data = data;
        }
    }

    public enum ENetServerOpcode
    {
        Stop,
        Restart
    }
}