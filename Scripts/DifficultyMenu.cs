using Godot;

public partial class DifficultyMenu : Control
{
    private Button easyButton;
    private Button mediumButton;
    private Button hardButton;
    private Button backButton;

    public override void _Ready()
    {
        //GD.Print("DifficultyMenu: Entering _Ready");

        easyButton = GetNode<Button>("DifficultyOptions/EasyButton");
        mediumButton = GetNode<Button>("DifficultyOptions/MediumButton");
        hardButton = GetNode<Button>("DifficultyOptions/HardButton");
        backButton = GetNode<Button>("DifficultyOptions/BackButton");

        if (easyButton == null || mediumButton == null || hardButton == null || backButton == null)
        {
            //GD.PrintErr("Error: Buttons not found in DifficultyMenu!");
            //GD.Print($"easy: {easyButton}, medium: {mediumButton}, hard: {hardButton}, back: {backButton}");
            return;
        }

        //GD.Print("DifficultyMenu: All buttons found");

        // Check button properties
        //GD.Print($"EasyButton - Visible: {easyButton.Visible}, Disabled: {easyButton.Disabled}, MouseFilter: {easyButton.MouseFilter}");
        //GD.Print($"MediumButton - Visible: {mediumButton.Visible}, Disabled: {mediumButton.Disabled}, MouseFilter: {mediumButton.MouseFilter}");
        //GD.Print($"HardButton - Visible: {hardButton.Visible}, Disabled: {hardButton.Disabled}, MouseFilter: {hardButton.MouseFilter}");
        //GD.Print($"BackButton - Visible: {backButton.Visible}, Disabled: {backButton.Disabled}, MouseFilter: {backButton.MouseFilter}");

        easyButton.Pressed += () =>
        {
            //GD.Print("EasyButton pressed");
            StartGame("bot", 0);
        };
        mediumButton.Pressed += () =>
        {
            //GD.Print("MediumButton pressed");
            StartGame("bot", 1);
        };
        hardButton.Pressed += () =>
        {
            //GD.Print("HardButton pressed");
            StartGame("bot", 2);
        };
        backButton.Pressed += () =>
        {
            //GD.Print("BackButton pressed");
            OnBackButtonPressed();
        };

        //GD.Print("DifficultyMenu initialized");
    }

    private void StartGame(string mode, int difficulty)
    {
        QueueFree();
        //GD.Print($"Starting game with mode: {mode}, difficulty: {difficulty}");
        var global = GetNode<Global>("/root/Global");
        if (global == null)
        {
            //GD.PrintErr("Error: Could not find Global at /root/Global");
            return;
        }
        global.GameMode = mode;
        global.BotDifficulty = difficulty;
        var error = GetTree().ChangeSceneToFile("res://Scenes/SinglePlayerGame.tscn");
        if (error != Error.Ok)
        {
            //GD.PrintErr($"Failed to load scene SinglePlayerGame.tscn: Error {error}");
        }
    }

    private void OnBackButtonPressed()
    {
        //GD.Print("Back button pressed!");
        QueueFree();
        var error = GetTree().ChangeSceneToFile("res://Scenes/Menu.tscn");
        if (error != Error.Ok)
        {
            //GD.PrintErr($"Failed to load scene Menu.tscn: Error {error}");
        }
    }
}