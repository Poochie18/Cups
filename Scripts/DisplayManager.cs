using Godot;

public partial class DisplayManager : Node
{
    // Параметры по умолчанию
    private Vector2I defaultWindowSize = new Vector2I(1920, 1080);
    private DisplayServer.WindowMode defaultWindowMode = DisplayServer.WindowMode.Windowed;
    private bool defaultBorderless = false;

    public override void _Ready()
    {
        // Применяем настройки окна при запуске
        ApplyWindowSettings();
    }

    /// <summary>
    /// Применяет настройки окна: размер, режим и рамку.
    /// </summary>
    public void ApplyWindowSettings()
    {
        bool isMobile = OS.GetName() == "Android" || OS.GetName() == "iOS";

        if (isMobile)
        {
            // Для мобильных устройств используем максимальный режим
            Vector2 screenSize = DisplayServer.ScreenGetSize();
            DisplayServer.WindowSetSize(new Vector2I((int)screenSize.X, (int)screenSize.Y));
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Maximized);
            GD.Print("Настройка окна для мобильных устройств: Максимальный режим.");
        }
        else
        {
            // Устанавливаем размер окна
            DisplayServer.WindowSetSize(defaultWindowSize);

            // Устанавливаем режим окна (например, Windowed)
            DisplayServer.WindowSetMode(defaultWindowMode);

            // Устанавливаем, будет ли окно без рамки
            DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, defaultBorderless);

            // Центрируем окно на экране
            CenterWindow();

            GD.Print($"Настройки окна применены: Размер={defaultWindowSize}, Режим={defaultWindowMode}, Без рамки={defaultBorderless}");
        }
    }

    /// <summary>
    /// Центрирует окно на экране.
    /// </summary>
    private void CenterWindow()
    {
        var screenSize = DisplayServer.ScreenGetSize();
        var windowPos = new Vector2I((screenSize.X - defaultWindowSize.X) / 2, (screenSize.Y - defaultWindowSize.Y) / 2);
        DisplayServer.WindowSetPosition(windowPos);
        GD.Print($"Окно центрировано: Позиция={windowPos}");
    }

    // Методы для изменения настроек (если нужно изменить их во время игры)

    public void SetWindowSize(Vector2I newSize)
    {
        defaultWindowSize = newSize;
        DisplayServer.WindowSetSize(newSize);
        CenterWindow();
        GD.Print($"Размер окна изменен: {newSize}");
    }

    public void SetWindowMode(DisplayServer.WindowMode mode)
    {
        defaultWindowMode = mode;
        DisplayServer.WindowSetMode(mode);
        GD.Print($"Режим окна изменен: {mode}");
    }

    public void SetBorderless(bool borderless)
    {
        defaultBorderless = borderless;
        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, borderless);
        GD.Print($"Настройка рамки изменена: Без рамки={borderless}");
    }

    // Геттеры для получения текущих настроек
    public Vector2I GetWindowSize()
    {
        return defaultWindowSize;
    }

    public DisplayServer.WindowMode GetWindowMode()
    {
        return defaultWindowMode;
    }

    public bool IsBorderless()
    {
        return defaultBorderless;
    }
}