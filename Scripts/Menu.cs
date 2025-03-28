using Godot;

public partial class Menu : Control
{
    private Button playVsFriendButton;
    private Button playVsBotButton;
    private Button multiplayerButton;
    private Button settingsButton;
    private Button exitButton;
    private VBoxContainer mainMenuContainer;

    public override void _Ready()
    {
        DisplayServer.WindowSetSize(new Vector2I(1920, 1080));
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        // Инициализация узлов
        mainMenuContainer = GetNode<VBoxContainer>("MainMenu");
        playVsFriendButton = GetNode<Button>("MainMenu/PlayVsFriendButton");
        playVsBotButton = GetNode<Button>("MainMenu/PlayVsBotButton");
        multiplayerButton = GetNode<Button>("MainMenu/MultiplayerButton");
        settingsButton = GetNode<Button>("MainMenu/SettingsButton");
        exitButton = GetNode<Button>("MainMenu/ExitButton");

        if (mainMenuContainer == null || playVsFriendButton == null || 
            playVsBotButton == null || multiplayerButton == null || 
            settingsButton == null || exitButton == null)
        {
            GD.PrintErr("Ошибка: Не найдены узлы меню!");
            return;
        }

        // Настройка кнопок
        foreach (Node child in mainMenuContainer.GetChildren())
        {
            if (child is Button btn)
            {
                btn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                btn.SizeFlagsVertical = SizeFlags.ShrinkCenter;
                //btn.AddThemeFontSizeOverride("font_size", CalculateFontSize());
            }
        }

        // Привязка событий
        playVsFriendButton.Pressed += OnFriendButtonPressed;
        playVsBotButton.Pressed += OnBotButtonPressed;
        multiplayerButton.Pressed += OnMultiplayerButtonPressed;
        settingsButton.Pressed += OnSettingsButtonPressed;
        exitButton.Pressed += OnExitButtonPressed;

        mainMenuContainer.Visible = true;
        GD.Print("Menu initialized. Screen size: ", GetViewportRect().Size);
    }

    private void OnFriendButtonPressed()
    {
        GD.Print("Friend button pressed!");
        var global = GetNode<Global>("/root/Global");
        global.GameMode = "friend";
        LoadScene("res://Scenes/SinglePlayerGame.tscn");
    }

    private void OnBotButtonPressed()
    {
        GD.Print("Bot button pressed!");
        LoadScene("res://Scenes/DifficultyMenu.tscn");
    }

    private void OnMultiplayerButtonPressed()
    {
        GD.Print("Multiplayer button pressed!");
        var global = GetNode<Global>("/root/Global");
        global.GameMode = "multiplayer";
        LoadScene("res://Scenes/MultiplayerMenu.tscn");
    }

    private void OnSettingsButtonPressed()
    {
        GD.Print("Settings button pressed!");
        LoadScene("res://Scenes/Settings.tscn");
    }

    private void OnExitButtonPressed()
    {
        GD.Print("Exit button pressed!");
        GetTree().Quit();
    }

    private void LoadScene(string path)
    {
        GD.Print($"Attempting to load scene: {path}");
        PackedScene scene = GD.Load<PackedScene>(path);
        if (scene != null)
        {
            Node sceneInstance = scene.Instantiate();
            GetTree().Root.AddChild(sceneInstance);
            GD.Print($"Scene {path} loaded and added to tree");
            QueueFree();
        }
        else
        {
            GD.PrintErr($"Ошибка: Не удалось загрузить сцену {path}!");
        }
    }

    private int CalculateFontSize()
    {
        Vector2 screenSize = GetViewport().GetVisibleRect().Size;
        return Mathf.Clamp((int)(screenSize.Y / 20f), 24, 64);
    }
}