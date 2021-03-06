using Godot;
using GodotModules.Netcode.Server;
using System;

namespace Game
{
    public class SceneGame : AScene
    {
        [Export] public readonly NodePath NodePathLabelPlayerHealth;
        public Label LabelPlayerHealth;

        public Dictionary<byte, OtherPlayer> Players;
        public Dictionary<ushort, Enemy> Enemies;
        public ClientPlayer Player { get; set; }

        public PrevCurQueue<Dictionary<ushort, DataEnemy>> EnemyTransformQueue { get; set; }
        private PrevCurQueue<Dictionary<byte, DataEntityTransform>> PlayerTransformQueue { get; set; }

        private List<Sprite> Bullets = new List<Sprite>();

        public override void _Ready()
        {
            LabelPlayerHealth = GetNode<Label>(NodePathLabelPlayerHealth);

            Players = new();
            Enemies = new();

            Player = Prefabs.ClientPlayer.Instance<ClientPlayer>();
            Player.Position = Vector2.Zero;

            AddChild(Player);
            PlayerTransformQueue = new PrevCurQueue<Dictionary<byte, DataEntityTransform>>(ServerIntervals.PlayerTransforms);
            EnemyTransformQueue = new PrevCurQueue<Dictionary<ushort, DataEnemy>>(ServerIntervals.PlayerTransforms);

            var bullet = Prefabs.Bullet.Instance<Sprite>();
            Bullets.Add(bullet);
            AddChild(bullet);

            // set game definitions
            GM.ModLoader.Script.Globals["Player", "setHealth"] = (Action<int>)Player.SetHealth;

            GM.ModLoader.Call("OnGameInit");

            if (NetworkManager.IsMultiplayer())
                InitMultiplayerStuff();
        }

        public override void _PhysicsProcess(float delta)
        {
            GM.ModLoader.Call("OnGameUpdate", delta);

            if (SceneManager.PrevSceneName == "Menu") // singleplayer
                return;
            
            foreach (var bullet in Bullets)
                bullet.Position += new Vector2(0, -1f);

            PlayerTransformQueue.UpdateProgress(delta);
            EnemyTransformQueue.UpdateProgress(delta);

            if (PlayerTransformQueue.NotReady)
                return;

            if (EnemyTransformQueue.NotReady)
                return;

            foreach (var pair in PlayerTransformQueue.Current)
            {
                if (!Players.ContainsKey(pair.Key)) // TODO: Find more optimal approach for checking this
                    continue;

                if (!NetworkManager.ServerAuthoritativeMovement && NetworkManager.PeerId == pair.Key)
                    continue;

                var player = Players[pair.Key];

                var prev = PlayerTransformQueue.Previous[pair.Key];
                var cur = pair.Value;

                player.Position = Utils.Lerp(prev.Position, cur.Position, PlayerTransformQueue.Progress);
                player.PlayerSprite.Rotation = Utils.LerpAngle(player.PlayerSprite.Rotation, cur.Rotation, 0.05f);
            }

            foreach (var pair in EnemyTransformQueue.Current)
            {
                var enemy = Enemies[pair.Key];

                var prev = EnemyTransformQueue.Previous[pair.Key];
                var cur = pair.Value;

                enemy.Position = Utils.Lerp(prev.Position, cur.Position, EnemyTransformQueue.Progress);
            }
        }

        public override void _Input(InputEvent @event)
        {
            SceneManager.EscapePressed(async () => {
                if (SceneManager.PrevSceneName == "Menu")
                {
                    // Singleplayer
                    await SceneManager.ChangeScene("Menu", false);
                }
                else
                {
                    // Multiplayer
                    if (NetworkManager.GameClient != null)
                        NetworkManager.GameClient.Stop();
                    if (NetworkManager.GameServer != null)
                        NetworkManager.GameServer.Stop();
                }
            });
        }

        public void UpdatePlayerPositions(Dictionary<byte, DataEntityTransform> playerTransforms) => PlayerTransformQueue.Add(playerTransforms);

        public void RemovePlayer(byte id)
        {
            var player = Players[id];
            player.QueueFree();
            Players.Remove(id);
        }

        private void InitMultiplayerStuff()
        {
            Players[(byte)NetworkManager.PeerId] = Player;
            Player.SetUsername(GM.Options.OnlineUsername);

            bool IsNotClient(uint id) => id != NetworkManager.PeerId;

            NetworkManager.GameClient.Players
                .Where(x => IsNotClient(x.Key))
                .ForEach(pair =>
                {
                    var otherPlayer = Prefabs.OtherPlayer.Instance<OtherPlayer>();
                    otherPlayer.Position = Vector2.Zero;
                    Players.Add((byte)pair.Key, otherPlayer);
                    AddChild(otherPlayer);
                    otherPlayer.SetUsername(pair.Value);
                });
        }

        public override void Cleanup()
        {

        }
    }
}