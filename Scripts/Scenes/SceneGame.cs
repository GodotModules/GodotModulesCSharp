using Godot;

namespace GodotModules 
{
    public class SceneGame : AScene
    {
        [Export] public readonly NodePath NodePathNavigation2D;
        [Export] public readonly NodePath NodePathLine2D;
        public Navigation2D Navigation2D { get; set; }
        public Line2D Line2D { get; set; }

        private GameManager _gameData;

        public override void PreInit(Managers managers)
        {
            
        }

        public override void _Ready()
        {
            Navigation2D = GetNode<Navigation2D>(NodePathNavigation2D);
            Line2D = GetNode<Line2D>(NodePathLine2D);
            _gameData = new GameManager(this);
            _gameData.CreateMainPlayer();
            _gameData.CreateEnemy(new Vector2(200, 200));
        }
    }

    public class GameManager
    {
        public List<OtherPlayer> Players { get; set; }
        public Player Player { get; set; }
        public List<Enemy> Enemies { get; set; }

        private SceneGame _sceneGame;

        public GameManager(SceneGame sceneGame)
        {
            _sceneGame = sceneGame;
            Enemies = new();
            Players = new();
        }

        public void CreateMainPlayer(Vector2 pos = default(Vector2))
        {
            var player = Prefabs.Player.Instance<Player>();
            player.Position = pos;
            _sceneGame.AddChild(player);
            Player = player;
            Players.Add(player);
        }

        public void CreateOtherPlayer(Vector2 pos = default(Vector2))
        {
            var otherPlayer = Prefabs.OtherPlayer.Instance<OtherPlayer>();
            otherPlayer.Position = pos;
            _sceneGame.AddChild(otherPlayer);
            Players.Add(otherPlayer);
        }

        public void CreateEnemy(Vector2 pos = default(Vector2))
        {
            var enemy = Prefabs.Enemy.Instance<Enemy>();
            enemy.Init(Players, _sceneGame.Navigation2D, _sceneGame.Line2D);
            enemy.Position = pos;
            _sceneGame.AddChild(enemy);
            Enemies.Add(enemy);
        }
    }
}
