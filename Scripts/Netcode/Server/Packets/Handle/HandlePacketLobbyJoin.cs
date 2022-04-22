using GodotModules.Netcode;
using ENet;
using System.Collections.Generic;
using System.Linq;

namespace GodotModules.Netcode.Server
{
    public class HandlePacketLobbyJoin : HandlePacket
    {
        public override void Handle(Peer peer, PacketReader reader)
        {
            var data = new CPacketLobbyJoin();
            data.Read(reader);

            // Check if data.Username is appropriate username
            // TODO

            // Keep track of joining player server side
            if (Players.ContainsKey(peer.ID)) 
            {
                Log($"Received LobbyJoin packet from peer with id {peer.ID}. Tried to add id {peer.ID} to Players but exists already");
                return;
            }

            Players.Add(peer.ID, data.Username);

            // tell joining player their Id and tell them about other players in lobby
            Send(ServerPacketOpcode.LobbyInfo, new SPacketLobbyInfo {
                Id = peer.ID,
                Players = GetOtherPlayers(peer.ID)
            }, peer);

            // tell other players about new player that joined
            Send(ServerPacketOpcode.LobbyJoin, new SPacketLobbyJoin {
                Id = peer.ID,
                Username = data.Username
            }, GetOtherPeers(peer.ID));
        }
    }
}