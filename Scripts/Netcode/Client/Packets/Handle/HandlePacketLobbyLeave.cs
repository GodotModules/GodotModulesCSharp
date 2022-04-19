using Common.Netcode;
using ENet;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace GodotModules.Netcode.Client
{
    public class HandlePacketLobbyLeave : HandlePacket
    {
        public override void Handle(PacketReader reader)
        {
            var data = new RPacketLobbyLeave(reader);

            if (!GameManager.GameClient.Players.ContainsKey(data.Id))
            {
                GD.Print($"Received LobbyLeave packet from server for id {data.Id}. Tried to remove from Players but does not exist in Players to begin with");
                return;
            }

            UILobby.RemovePlayer(data.Id);

            GD.Print($"Player with id: {data.Id} left the lobby");
        }
    }
}