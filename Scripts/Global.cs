using Godot;

public partial class Global : Node
{
    public string GameMode { get; set; } = "friend";
    public int BotDifficulty { get; set; } = 0;

    public override void _Ready()
    {
        GD.Print("Global script initialized");
    }
}