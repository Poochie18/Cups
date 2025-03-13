using Godot;

public partial class DifficultyMenu : Control
{
    private Button easyButton;
    private Button mediumButton;
    private Button hardButton;
    private Button backButton;

    public override void _Ready()
    {
        easyButton = GetNode<Button>("DifficultyOptions/EasyButton");
        mediumButton = GetNode<Button>("DifficultyOptions/MediumButton");
        hardButton = GetNode<Button>("DifficultyOptions/HardButton");
        backButton = GetNode<Button>("DifficultyOptions/BackButton");

        if (easyButton == null || mediumButton == null || hardButton == null || backButton == null)
        {
            GD.PrintErr("Ошибка: Не найдены кнопки в DifficultyMenu!");
            GD.Print($"easy: {easyButton}, medium: {mediumButton}, hard: {hardButton}, back: {backButton}");
            return;
        }

        easyButton.Pressed += () => StartGame("bot", 0);
        mediumButton.Pressed += () => StartGame("bot", 1);
        hardButton.Pressed += () => StartGame("bot", 2);
        backButton.Pressed += OnBackButtonPressed;

        GD.Print("DifficultyMenu initialized");
    }

    private void StartGame(string mode, int difficulty)
    {
        GD.Print($"Starting game with mode: {mode}, difficulty: {difficulty}");
        PackedScene gameScene = GD.Load<PackedScene>("res://Scenes/Game.tscn");
        if (gameScene != null)
        {
            GD.Print("Game.tscn loaded successfully");
            Game gameInstance = gameScene.Instantiate<Game>();
            if (gameInstance != null)
            {
                GD.Print("Game instance created");
                gameInstance.SetGameMode(mode, difficulty);
                GetTree().Root.AddChild(gameInstance);
                GD.Print("Game instance added to scene tree");
                QueueFree();
            }
            else
            {
                GD.PrintErr("Ошибка: Не удалось создать экземпляр Game.tscn!");
            }
        }
        else
        {
            GD.PrintErr("Ошибка: Не удалось загрузить Game.tscn!");
        }
    }

    private void OnBackButtonPressed()
    {
        GD.Print("Back button pressed!");
        LoadScene("res://Scenes/Menu.tscn"); // Исправлен путь
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
}