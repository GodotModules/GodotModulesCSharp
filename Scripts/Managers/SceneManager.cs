using Godot;

namespace GodotModules
{
    public class SceneManager
    {
        public readonly Dictionary<GameScene, Action<Node>> PreInit = new();
        public readonly Dictionary<GameScene, Action> EscPressed = new Dictionary<GameScene, Action>();

        public GameScene CurScene { get; set; }
        public GameScene PrevScene { get; set; }

        private Node _activeScene;
        private readonly Dictionary<GameScene, PackedScene> _scenes = new Dictionary<GameScene, PackedScene>();
        private readonly GodotFileManager _godotFileManager;
        private readonly HotkeyManager _hotkeyManager;
        private readonly Control _sceneList;
        private Managers _managers;

        public SceneManager(Control sceneList, GodotFileManager godotFileManager, HotkeyManager hotkeyManager, Managers managers) 
        {
            _sceneList = sceneList;
            _godotFileManager = godotFileManager;
            _hotkeyManager = hotkeyManager;
            _managers = managers;
        }

        public async Task InitAsync()
        {
            var loadedScenes = _godotFileManager.LoadDir("Scenes/Scenes", (dir, fileName) =>
            {
                if (!dir.CurrentIsDir())
                    LoadScene(fileName.Replace(".tscn", ""));
            });

            if (loadedScenes)
                await ChangeScene(GameScene.Menu);
        }

        public async Task ChangeScene(GameScene scene, bool instant = true)
        {
            if (CurScene == scene)
                return;
                
            PrevScene = CurScene;
            CurScene = scene;

            if (_sceneList.GetChildCount() != 0) 
                _sceneList.GetChild(0).QueueFree();

            if (!instant)
                await Task.Delay(1);

            _activeScene = _scenes[scene].Instance();

            if (PreInit.ContainsKey(scene))
                PreInit[scene](_activeScene);

            if (_activeScene is AScene ascene)
                ascene.PreInitManagers(_managers);

            _sceneList.AddChild(_activeScene);
        }

        private void LoadScene(string scene) 
        {
            try 
            {
                _scenes[(GameScene)Enum.Parse(typeof(GameScene), scene)] = ResourceLoader.Load<PackedScene>($"res://Scenes/Scenes/{scene}.tscn");
            }
            catch (ArgumentException) 
            {
                Logger.LogWarning($"Enum for {scene} needs to be defined since the scene is in the Scenes directory");
            }
        }
    }

    public enum GameScene
    {
        Game,
        Game3D,
        GameServers,
        Lobby,
        Menu,
        Options,
        Mods,
        Credits
    }
}