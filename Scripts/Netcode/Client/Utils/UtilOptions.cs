using Godot;

namespace GodotModules
{
    public class UtilOptions
    {
        public static bool OptionsCreatedForFirstTime { get; set; }

        public static void InitOptions()
        {
            if (!System.IO.File.Exists(PathOptions))
            {
                OptionsCreatedForFirstTime = true;
                SystemFileManager.WriteConfig<OptionsData>(PathOptions);
            }

            GM.Options = SystemFileManager.GetConfig<OptionsData>(PathOptions);
            SupportedResolutions = GetSupportedResolutions();

            if (OptionsCreatedForFirstTime)
            {
                // defaults
                GM.Options = new OptionsData
                {
                    Resolution = SupportedResolutions.Count - 1,
                    FullscreenMode = 0,
                    VolumeMusic = -15,
                    VolumeSFX = -15,
                    VSync = true,
                    OnlineUsername = ""
                };
            }

            ApplyOptions();
        }

        public static void CenterWindow() => OS.WindowPosition = OS.GetScreenSize() / 2 - OS.WindowSize / 2;

        public static void ApplyOptions()
        {
            // apply settings
            if (GM.Options.FullscreenMode == FullscreenMode.Windowed)
            {
                OS.WindowSize = SupportedResolutions[GM.Options.Resolution];
                CenterWindow();
            }
            else
                SetFullscreenMode(GM.Options.FullscreenMode);

            MusicManager.SetVolumeValue(GM.Options.VolumeMusic);

            OS.VsyncEnabled = GM.Options.VSync;
        }

        public static void SetFullscreenBorderless()
        {
            OS.WindowFullscreen = false;
            OS.WindowBorderless = true;
            OS.WindowPosition = new Vector2(0, 0);
            OS.WindowSize = OS.GetScreenSize() + new Vector2(1, 1); // need to add (1, 1) otherwise will act like fullscreen mode (seems like a Godot bug)
        }

        public static void SetFullscreenMode(FullscreenMode mode)
        {
            switch (mode)
            {
                case FullscreenMode.Windowed:
                    SetWindowedMode();
                    break;

                case FullscreenMode.Borderless:
                    SetFullscreenBorderless();
                    break;

                case FullscreenMode.Fullscreen:
                    OS.WindowFullscreen = true;
                    break;
            }

            GM.Options.FullscreenMode = mode;
        }

        public static void ToggleFullscreen()
        {
            // for when F11 or Alt+Enter are pressed
            var mode = GM.Options.FullscreenMode;

            switch (mode)
            {
                case FullscreenMode.Windowed:
                    SetFullscreenMode(FullscreenMode.Borderless);
                    break;

                case FullscreenMode.Borderless:
                case FullscreenMode.Fullscreen:
                    SetFullscreenMode(FullscreenMode.Windowed);
                    break;
            }
        }

        public static int CurrentResolution { get; set; }

        public static void SetWindowedMode()
        {
            OS.WindowFullscreen = false;
            OS.WindowBorderless = false;
            OS.WindowSize = SupportedResolutions[CurrentResolution];
            CenterWindow();
        }

        public static Dictionary<int, Vector2> SupportedResolutions { get; set; }

        /// <summary>
        /// Godot does not provide any functions to print the list of supported resolutions from Monitor[]
        /// This gets the supported resolutions based off OS.GetScreenSize()
        /// </summary>
        /// <returns>The supported resolutions of the monitor</returns>
        private static Dictionary<int, Vector2> GetSupportedResolutions()
        {
            var supportedResolutions = new Dictionary<int, Vector2>();

            var maxSize = OS.GetScreenSize();

            var resolutions = new Vector2[] {
                new Vector2(426, 240),
                new Vector2(640, 360),
                new Vector2(854, 480),
                new Vector2(1280, 720),
                new Vector2(1920, 1080),
                new Vector2(2560, 1440),
                new Vector2(3840, 2160),
                new Vector2(7680, 4320)
            };

            for (int i = 0; i < resolutions.Length; i++)
                if (resolutions[i] <= maxSize)
                    supportedResolutions.Add(i, resolutions[i]);

            return supportedResolutions;
        }

        public static string PathOptions => System.IO.Path.Combine(GM.GetGameDataPath(), "options.json");

        public static void SaveOptions() => SystemFileManager.WriteConfig(PathOptions, GM.Options);
    }

    public enum FullscreenMode
    {
        Windowed,
        Borderless,
        Fullscreen
    }

    public class OptionsData
    {
        public FullscreenMode FullscreenMode { get; set; }
        public int Resolution { get; set; }
        public float VolumeMusic { get; set; }
        public float VolumeSFX { get; set; }
        public bool VSync { get; set; }
        public string OnlineUsername { get; set; }
    }
}