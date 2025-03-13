using Godot;

public partial class Menu : Control
{
    private Button playVsFriendButton;
    private Button playVsBotButton;
    private Button multiplayerButton;
    private VBoxContainer mainMenuContainer;

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

        playVsFriendButton.Pressed += OnPlayVsFriendButtonPressed;
        playVsBotButton.Pressed += OnPlayVsBotButtonPressed;
        multiplayerButton.Pressed += OnMultiplayerButtonPressed;

        mainMenuContainer.Visible = true;

        GD.Print("Menu initialized. Screen size: ", GetViewportRect().Size);
    }

    private void OnPlayVsFriendButtonPressed()
    {
        GD.Print("Play vs Friend button pressed!");
        StartGame("friend");
    }

    private void OnPlayVsBotButtonPressed()
    {
        GD.Print("Play vs Bot button pressed!");
        LoadScene("res://Scenes/DifficultyMenu.tscn");
    }

    private void OnMultiplayerButtonPressed()
    {
        GD.Print("Multiplayer button pressed!");
        LoadScene("res://Scenes/MultiplayerMenu.tscn");
    }

    private void StartGame(string mode, int difficulty = 0)
    {
        GD.Print($"Starting game with mode: {mode}, difficulty: {difficulty}");
        PackedScene gameScene = GD.Load<PackedScene>("res://Scenes/Game.tscn");
        if (gameScene != null)
        {
            Game gameInstance = gameScene.Instantiate<Game>();
            gameInstance.SetGameMode(mode, difficulty);
            GetTree().Root.AddChild(gameInstance);
            QueueFree();
        }
        else
        {
            GD.PrintErr("Ошибка: Не удалось загрузить Game.tscn!");
        }
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