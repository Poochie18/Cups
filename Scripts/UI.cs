using Godot;

public partial class UI : Control
{
    private Label statusLabel;
    private Button menuButton;
    private Button restartButton;

    public override void _Ready()
    {
        statusLabel = GetNode<Label>("StatusLabel");
        menuButton = GetNode<Button>("BackToMenuButton");
        restartButton = GetNode<Button>("RestartButton");

        if (statusLabel == null || menuButton == null || restartButton == null)
        {
            //GD.PrintErr("Error: UI nodes not found!");
            //GD.Print($"statusLabel: {statusLabel}, menuButton: {menuButton}, restartButton: {restartButton}");
            return;
        }
    }

    public void UpdateStatus(string text)
    {
        if (statusLabel != null)
            statusLabel.Text = text;
    }

    public void OnMenuButtonPressed()
    {
        //GD.Print("Menu button pressed!");
        foreach (Node node in GetTree().Root.GetChildren())
        {
            if (node is Menu)
            {
                node.QueueFree();
            }
        }

        PackedScene menuScene = GD.Load<PackedScene>("res://Scenes/Menu.tscn");
        if (menuScene != null)
        {
            Node menuInstance = menuScene.Instantiate();
            GetTree().Root.AddChild(menuInstance);
            GetParent().QueueFree();
        }
        else
        {
            //GD.PrintErr("Error: Failed to load Menu.tscn!");
        }
    }

    public void OnRestartButtonPressed()
    {
        //GD.Print("Restart button pressed!");
        if (GetParent() is Game game)
        {
            if (game.gameMode == "multiplayer")
            {
                //GD.Print("Calling SyncResetGame for multiplayer");
                game.OnRestartButtonPressed();
            }
            else
            {
                game.ResetGame();
            }
        }
        else if (GetParent() is SinglePlayerGame singlePlayerGame)
        {
            singlePlayerGame.ResetGame();
        }
        else
        {
            //GD.PrintErr("UI.OnRestartButtonPressed: Failed to find Game or SinglePlayerGame!");
        }
    }
}