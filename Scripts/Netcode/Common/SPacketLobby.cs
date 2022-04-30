using GodotModules.Netcode.Client;
using GodotModules.Netcode.Server;
using System.Collections.Generic;

namespace GodotModules.Netcode
{
    public class SPacketLobby : APacketServerPeerId
    {
        public LobbyOpcode LobbyOpcode { get; set; }

        // Chat Message
        public string Message { get; set; }

        // Countdown Change
        public bool CountdownRunning { get; set; }

        // Info
        public bool IsHost { get; set; }
        public Dictionary<uint, DataPlayer> Players { get; set; }

        // Join
        public string Username { get; set; }

        // Ready
        public bool Ready { get; set; }

        public override void Write(PacketWriter writer)
        {
            writer.Write((byte)LobbyOpcode);

            switch (LobbyOpcode)
            {
                case LobbyOpcode.LobbyChatMessage:
                    base.Write(writer);
                    writer.Write((string)Message);
                    break;
                case LobbyOpcode.LobbyCountdownChange:
                    writer.Write(CountdownRunning);
                    break;
                case LobbyOpcode.LobbyInfo:
                    base.Write(writer);
                    writer.Write(IsHost);
                    writer.Write((ushort)Players.Count);
                    Players.ForEach(pair =>
                    {
                        writer.Write((ushort)pair.Key); // id
                        writer.Write((string)pair.Value.Username);
                    });
                    break;
                case LobbyOpcode.LobbyJoin:
                    base.Write(writer);
                    writer.Write((string)Username);
                    break;
                case LobbyOpcode.LobbyLeave:
                    base.Write(writer);
                    break;
                case LobbyOpcode.LobbyReady:
                    base.Write(writer);
                    writer.Write(Ready);
                    break;
            }
        }

        public override void Read(PacketReader reader)
        {
            LobbyOpcode = (LobbyOpcode)reader.ReadByte();

            switch (LobbyOpcode)
            {
                case LobbyOpcode.LobbyChatMessage:
                    base.Read(reader);
                    Message = reader.ReadString();
                    break;
                case LobbyOpcode.LobbyCountdownChange:
                    CountdownRunning = reader.ReadBool();
                    break;
                case LobbyOpcode.LobbyInfo:
                    base.Read(reader);
                    IsHost = reader.ReadBool();
                    var count = reader.ReadUShort();
                    Players = new Dictionary<uint, DataPlayer>();
                    for (int i = 0; i < count; i++)
                    {
                        var id = reader.ReadUShort();
                        var name = reader.ReadString();

                        Players.Add(id, new DataPlayer
                        {
                            Username = name,
                            Ready = false
                        });
                    }
                    break;
                case LobbyOpcode.LobbyJoin:
                    base.Read(reader);
                    Username = reader.ReadString();
                    break;
                case LobbyOpcode.LobbyLeave:
                    base.Read(reader);
                    break;
                case LobbyOpcode.LobbyReady:
                    base.Read(reader);
                    Ready = reader.ReadBool();
                    break;
            }
        }

        public override void Handle()
        {
            switch (LobbyOpcode)
            {
                case LobbyOpcode.LobbyChatMessage:
                    SceneLobby.Log(Id, Message);
                    break;
                case LobbyOpcode.LobbyCountdownChange:
                    if (CountdownRunning)
                        SceneLobby.StartGameCountdown();
                    else
                        SceneLobby.CancelGameCountdown();
                    break;
                case LobbyOpcode.LobbyGameStart:
                    if (GameClient.IsHost)
                        GameServer.EmitClientPositions.Enabled = true;

                    SceneManager.ChangeScene("Game");
                    break;
                case LobbyOpcode.LobbyInfo:
                    ENetClient.PeerId = Id;
                    ENetClient.IsHost = IsHost;
                    ENetClient.Log($"{GameManager.Options.OnlineUsername} joined lobby with id {Id}");
                    SceneLobby.AddPlayer(Id, GameManager.Options.OnlineUsername);

                    Players.ForEach(pair => SceneLobby.AddPlayer(pair.Key, pair.Value.Username));

                    SceneManager.ChangeScene("Lobby");
                    break;
                case LobbyOpcode.LobbyJoin:
                    SceneLobby.AddPlayer(Id, Username);

                    ENetClient.Log($"Player with username {Username} id: {Id} joined the lobby");
                    break;
                case LobbyOpcode.LobbyLeave:
                    SceneLobby.RemovePlayer(Id);

                    ENetClient.Log($"Player with id: {Id} left the lobby");
                    break;
                case LobbyOpcode.LobbyReady:
                    SceneLobby.SetReady(Id, Ready);
                    break;
            }
        }
    }
}