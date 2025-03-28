using Godot;

public partial class Global : Node
{
    public string GameMode { get; set; } = "friend";
    public int BotDifficulty { get; set; } = 0;
    public string PlayerNickname { get; set; } = "Player";
    public string OpponentNickname { get; set; } = "Opponent";

    private const string ConfigPath = "user://settings.cfg";

    public override void _Ready()
    {
        LoadSettings();
        if (OS.GetName() == "Android")
        {
            DisplayServer.WindowSetSize(new Vector2I(1280, 720));
            //GD.Print("Window size set to 1280x720 for Android (forcing landscape)");
        }
        //GD.Print("Global script initialized");
    }

    public void SaveSettings()
    {
        var config = new ConfigFile();
        config.SetValue("Player", "Nickname", PlayerNickname);
        Error err = config.Save(ConfigPath);
        if (err != Error.Ok)
        {
            //GD.PrintErr($"Error saving settings: {err}");
        }
        else
        {
            //GD.Print($"Settings saved: Nickname={PlayerNickname}");
        }
    }

    public void LoadSettings()
    {
        var config = new ConfigFile();
        Error err = config.Load(ConfigPath);
        if (err == Error.Ok)
        {
            PlayerNickname = config.GetValue("Player", "Nickname", "Player").AsString();
            //GD.Print($"Settings loaded: Nickname={PlayerNickname}");
        }
        else if (err != Error.FileNotFound)
        {
            //GD.PrintErr($"Error loading settings: {err}");
        }
    }
}