using Godot;

public partial class DifficultyMenu : Control
{
    private Button easyButton;
    private Button mediumButton;
    private Button hardButton;
    private Button backButton;

    public override void _Ready()
    {
        easyButton = GetNode<Button>("Frame/DifficultyOptions/EasyButton");
        mediumButton = GetNode<Button>("Frame/DifficultyOptions/MediumButton");
        hardButton = GetNode<Button>("Frame/DifficultyOptions/HardButton");
        backButton = GetNode<Button>("Frame/DifficultyOptions/BackButton");

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
        var global = GetNode<Global>("/root/Global");
        global.GameMode = mode;
        global.BotDifficulty = difficulty;
        LoadScene("res://Scenes/SinglePlayerGame.tscn");
    }

    private void OnBackButtonPressed()
    {
        GD.Print("Back button pressed!");
        
        LoadScene("res://Scenes/Menu.tscn");
    }

    private void LoadScene(string path)
    {
        GD.Print($"Attempting to load scene: {path}");
        PackedScene scene = GD.Load<PackedScene>(path);
        if (scene != null)
        {
            QueueFree();
            Node sceneInstance = scene.Instantiate();
            GetTree().Root.AddChild(sceneInstance);
            GD.Print($"Scene {path} loaded and added to tree");
            
        }
        else
        {
            GD.PrintErr($"Ошибка: Не удалось загрузить сцену {path}!");
        }
    }
}