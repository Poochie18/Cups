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
            GD.Print("Настройка окна для мобильных устройств: Максимальный режим.");
        }
        else
        {
            DisplayServer.WindowSetSize(defaultWindowSize);
            GD.Print($"Устанавливаем режим окна: {defaultWindowMode}");
            DisplayServer.WindowSetMode(defaultWindowMode);
            DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, defaultBorderless);
            if (defaultWindowMode == DisplayServer.WindowMode.Windowed)
            {
                CenterWindow();
            }
            GD.Print($"Настройки окна применены: Размер={defaultWindowSize}, Режим={defaultWindowMode}, Без рамки={defaultBorderless}");
        }
    }

    private void CenterWindow()
    {
        var screenSize = DisplayServer.ScreenGetSize();
        var windowPos = new Vector2I((screenSize.X - defaultWindowSize.X) / 2, (screenSize.Y - defaultWindowSize.Y) / 2);
        DisplayServer.WindowSetPosition(windowPos);
        GD.Print($"Окно центрировано: Позиция={windowPos}");
    }

    private void LoadSettings()
    {
        configFile = new ConfigFile();
        Error error = configFile.Load(SettingsPath);
        if (error != Error.Ok)
        {
            GD.Print($"Не удалось загрузить настройки: {error}. Используются значения по умолчанию.");
            return;
        }

        string modeString = (string)configFile.GetValue("Display", "WindowMode", "Windowed");
        defaultWindowMode = modeString == "Fullscreen" ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed;
        defaultBorderless = (bool)configFile.GetValue("Display", "Borderless", false);

        GD.Print($"Настройки загружены: Режим={defaultWindowMode}, Без рамки={defaultBorderless}");
    }

    public void SaveSettings()
    {
        configFile = new ConfigFile();

        string modeString = defaultWindowMode == DisplayServer.WindowMode.Fullscreen ? "Fullscreen" : "Windowed";
        configFile.SetValue("Display", "WindowMode", modeString);
        configFile.SetValue("Display", "Borderless", defaultBorderless);

        GD.Print($"Сохраняем настройки: WindowMode={modeString}, Borderless={defaultBorderless}");
        Error error = configFile.Save(SettingsPath);
        if (error != Error.Ok)
        {
            GD.PrintErr($"Ошибка при сохранении настроек: {error}");
        }
        else
        {
            GD.Print("Настройки сохранены успешно.");
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
        GD.Print($"Размер окна изменен: {newSize}");
    }

    public void SetWindowMode(DisplayServer.WindowMode mode)
    {
        GD.Print($"SetWindowMode вызван: Новый режим={mode}, Текущий режим={defaultWindowMode}");
        defaultWindowMode = mode;
        DisplayServer.WindowSetMode(mode);
        if (mode == DisplayServer.WindowMode.Windowed)
        {
            CenterWindow();
        }
        SaveSettings();
        GD.Print($"Режим окна изменен: {mode}");
        // Дополнительная проверка: убедимся, что режим применился
        var actualMode = DisplayServer.WindowGetMode();
        GD.Print($"Фактический режим окна после изменения: {actualMode}");
        if (actualMode != mode)
        {
            GD.PrintErr($"Ошибка: Режим окна не изменился! Ожидалось: {mode}, Фактически: {actualMode}");
        }
    }

    public void SetBorderless(bool borderless)
    {
        defaultBorderless = borderless;
        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, borderless);
        SaveSettings();
        GD.Print($"Настройка рамки изменена: Без рамки={borderless}");
    }

    public Vector2I GetWindowSize()
    {
        return defaultWindowSize;
    }

    public DisplayServer.WindowMode GetWindowMode()
    {
        GD.Print($"GetWindowMode вызван: Возвращаем режим={defaultWindowMode}");
        return defaultWindowMode;
    }

    public bool IsBorderless()
    {
        return defaultBorderless;
    }
}