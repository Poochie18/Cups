using Godot;

public partial class Global : Node
{
    public string GameMode { get; set; } = "friend";
    public int BotDifficulty { get; set; } = 0;
    public string PlayerNickname { get; set; } = "Player"; // Никнейм локального игрока
    public string OpponentNickname { get; set; } = "Opponent"; // Никнейм оппонента (для мультиплеера)

    public override void _Ready()
    {
        GD.Print("Global script initialized");
    }
}