using Godot;

public partial class Global : Node
{
    public string GameMode { get; set; } = "friend";
    public int BotDifficulty { get; set; } = 0;
    public string PlayerNickname { get; set; } = "Player";
    public string OpponentNickname { get; set; } = "Opponent";

    private const string ConfigPath = "user://settings.cfg"; // Путь к файлу настроек

    public override void _Ready()
    {
        LoadSettings(); // Загружаем настройки при старте
        if (OS.GetName() == "Android")
        {
            // Устанавливаем размер окна в горизонтальной ориентации
            DisplayServer.WindowSetSize(new Vector2I(1280, 720));
            GD.Print("Window size set to 1280x720 for Android (forcing landscape)");
        }
        GD.Print("Global script initialized");
    }

    public void SaveSettings()
    {
        var config = new ConfigFile();
        config.SetValue("Player", "Nickname", PlayerNickname);
        Error err = config.Save(ConfigPath);
        if (err != Error.Ok)
        {
            GD.PrintErr($"Ошибка сохранения настроек: {err}");
        }
        else
        {
            GD.Print($"Настройки сохранены: Nickname={PlayerNickname}");
        }
    }

    public void LoadSettings()
    {
        var config = new ConfigFile();
        Error err = config.Load(ConfigPath);
        if (err == Error.Ok)
        {
            PlayerNickname = config.GetValue("Player", "Nickname", "Player").AsString();
            GD.Print($"Настройки загружены: Nickname={PlayerNickname}");
        }
        else if (err != Error.FileNotFound)
        {
            GD.PrintErr($"Ошибка загрузки настроек: {err}");
        }
    }
}