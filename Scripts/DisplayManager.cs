using Godot;

public partial class DisplayManager : Node
{
    private Vector2I defaultWindowSize = new Vector2I(1920, 1080);
    private DisplayServer.WindowMode defaultWindowMode = DisplayServer.WindowMode.Windowed;
    private bool defaultBorderless = false;

    private const string SettingsPath = "user://settings.cfg";
    private ConfigFile configFile;

    public override void _Ready()
    {
        LoadSettings();
        ApplyWindowSettings();
    }

    public void ApplyWindowSettings()
    {
        bool isMobile = OS.GetName() == "Android" || OS.GetName() == "iOS";

        if (isMobile)
        {
            Vector2 screenSize = DisplayServer.ScreenGetSize();
            DisplayServer.WindowSetSize(new Vector2I((int)screenSize.X, (int)screenSize.Y));
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Maximized);
            //GD.Print("Window setup for mobile devices: Maximized mode.");
        }
        else
        {
            DisplayServer.WindowSetSize(defaultWindowSize);
            //GD.Print($"Setting window mode: {defaultWindowMode}");
            DisplayServer.WindowSetMode(defaultWindowMode);
            DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, defaultBorderless);
            if (defaultWindowMode == DisplayServer.WindowMode.Windowed)
            {
                CenterWindow();
            }
            //GD.Print($"Window settings applied: Size={defaultWindowSize}, Mode={defaultWindowMode}, Borderless={defaultBorderless}");
        }
    }

    private void CenterWindow()
    {
        var screenSize = DisplayServer.ScreenGetSize();
        var windowPos = new Vector2I((screenSize.X - defaultWindowSize.X) / 2, (screenSize.Y - defaultWindowSize.Y) / 2);
        DisplayServer.WindowSetPosition(windowPos);
        //GD.Print($"Window centered: Position={windowPos}");
    }

    private void LoadSettings()
    {
        configFile = new ConfigFile();
        Error error = configFile.Load(SettingsPath);
        if (error != Error.Ok)
        {
            //GD.Print($"Failed to load settings: {error}. Using default values.");
            return;
        }

        string modeString = (string)configFile.GetValue("Display", "WindowMode", "Windowed");
        defaultWindowMode = modeString == "Fullscreen" ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed;
        defaultBorderless = (bool)configFile.GetValue("Display", "Borderless", false);

        //GD.Print($"Settings loaded: Mode={defaultWindowMode}, Borderless={defaultBorderless}");
    }

    public void SaveSettings()
    {
        configFile = new ConfigFile();

        string modeString = defaultWindowMode == DisplayServer.WindowMode.Fullscreen ? "Fullscreen" : "Windowed";
        configFile.SetValue("Display", "WindowMode", modeString);
        configFile.SetValue("Display", "Borderless", defaultBorderless);

        //GD.Print($"Saving settings: WindowMode={modeString}, Borderless={defaultBorderless}");
        Error error = configFile.Save(SettingsPath);
        if (error != Error.Ok)
        {
            //GD.PrintErr($"Error saving settings: {error}");
        }
        else
        {
            //GD.Print("Settings saved successfully.");
        }
    }

    public void SetWindowSize(Vector2I newSize)
    {
        defaultWindowSize = newSize;
        DisplayServer.WindowSetSize(newSize);
        if (defaultWindowMode == DisplayServer.WindowMode.Windowed)
        {
            CenterWindow();
        }
        //GD.Print($"Window size changed: {newSize}");
    }

    public void SetWindowMode(DisplayServer.WindowMode mode)
    {
        //GD.Print($"SetWindowMode called: New mode={mode}, Current mode={defaultWindowMode}");
        defaultWindowMode = mode;
        DisplayServer.WindowSetMode(mode);
        if (mode == DisplayServer.WindowMode.Windowed)
        {
            CenterWindow();
        }
        SaveSettings();
        //GD.Print($"Window mode changed: {mode}");
        // Additional check: ensure the mode was applied
        var actualMode = DisplayServer.WindowGetMode();
        //GD.Print($"Actual window mode after change: {actualMode}");
        if (actualMode != mode)
        {
            //GD.PrintErr($"Error: Window mode did not change! Expected: {mode}, Actual: {actualMode}");
        }
    }

    public void SetBorderless(bool borderless)
    {
        defaultBorderless = borderless;
        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, borderless);
        SaveSettings();
        //GD.Print($"Border setting changed: Borderless={borderless}");
    }

    public Vector2I GetWindowSize()
    {
        return defaultWindowSize;
    }

    public DisplayServer.WindowMode GetWindowMode()
    {
        //GD.Print($"GetWindowMode called: Returning mode={defaultWindowMode}");
        return defaultWindowMode;
    }

    public bool IsBorderless()
    {
        return defaultBorderless;
    }
}