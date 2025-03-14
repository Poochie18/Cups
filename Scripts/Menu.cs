using Godot;

public partial class Menu : Control
{
    private Button playVsFriendButton;
    private Button playVsBotButton;
    private Button multiplayerButton;
    private VBoxContainer mainMenuContainer;
    private int botDifficulty = 0; // Значение по умолчанию для сложности бота

    public override void _Ready()
    {
        mainMenuContainer = GetNode<VBoxContainer>("MainMenu");
        playVsFriendButton = GetNode<Button>("MainMenu/PlayVsFriendButton");
        playVsBotButton = GetNode<Button>("MainMenu/PlayVsBotButton");
        multiplayerButton = GetNode<Button>("MainMenu/MultiplayerButton");

        if (mainMenuContainer == null || playVsFriendButton == null || 
            playVsBotButton == null || multiplayerButton == null)
        {
            GD.PrintErr("Ошибка: Не найдены узлы меню!");
            GD.Print($"mainMenu: {mainMenuContainer}");
            GD.Print($"playVsFriend: {playVsFriendButton}, playVsBot: {playVsBotButton}, multiplayer: {multiplayerButton}");
            return;
        }

        // Исправляем имена методов в привязке событий
        playVsFriendButton.Pressed += OnFriendButtonPressed;
        playVsBotButton.Pressed += OnBotButtonPressed;
        multiplayerButton.Pressed += OnMultiplayerButtonPressed;

        mainMenuContainer.Visible = true;

        GD.Print("Menu initialized. Screen size: ", GetViewportRect().Size);
    }

    private void OnFriendButtonPressed()
    {
        GD.Print("Friend button pressed!");
        GetTree().ChangeSceneToFile("res://Scenes/SinglePlayerGame.tscn");
        // Настройка режима происходит в сцене, GetNode здесь не сработает сразу после смены сцены
    }

    private void OnBotButtonPressed()
    {
        GD.Print("Bot button pressed!");
        GetTree().ChangeSceneToFile("res://Scenes/SinglePlayerGame.tscn");
        // Настройка режима происходит в сцене
    }

    private void OnMultiplayerButtonPressed()
    {
        GD.Print("Multiplayer button pressed!");
        LoadScene("res://Scenes/MultiplayerMenu.tscn");
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
            QueueFree(); // Удаляем текущую сцену
        }
        else
        {
            GD.PrintErr($"Ошибка: Не удалось загрузить сцену {path}!");
        }
    }
}