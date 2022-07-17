using LiteNetLib;
using GodotModules.Netcode.Server;

namespace GodotModules.Netcode
{
    public class CPacketPlayerPosition : APacketClient
    {
        public Vector2 Position { get; set; }

        public override void Write(PacketWriter writer)
        {
            writer.Write(Position);
        }

        public override void Read(PacketReader reader)
        {
            Position = reader.ReadVector2();
        }

        public override void Handle(GameServer server, NetPeer peer)
        {
            server.Players[(byte)peer.Id].Position = Position;
        }
    }
}