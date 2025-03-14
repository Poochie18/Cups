using Godot;

public partial class DifficultyMenu : Control
{
    private Button easyButton;
    private Button mediumButton;
    private Button hardButton;

    public override void _Ready()
    {
        easyButton = GetNode<Button>("EasyButton");
        mediumButton = GetNode<Button>("MediumButton");
        hardButton = GetNode<Button>("HardButton");

        if (easyButton == null || mediumButton == null || hardButton == null)
        {
            GD.PrintErr("Ошибка: Не найдены кнопки в DifficultyMenu!");
            return;
        }

        easyButton.Pressed += () => StartGame(0);   // Легкий
        mediumButton.Pressed += () => StartGame(1); // Средний
        hardButton.Pressed += () => StartGame(2);   // Сложный
    }

    private void StartGame(int difficulty)
    {
        GD.Print($"Starting game with bot, difficulty: {difficulty}");
        var global = GetNode<Global>("/root/Global");
        global.GameMode = "bot";
        global.BotDifficulty = difficulty;
        GetTree().ChangeSceneToFile("res://Scenes/SinglePlayerGame.tscn");
    }
}