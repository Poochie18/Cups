using Godot;

public partial class MultiplayerMenu : Control
{
    private Button createRoomButton;
    private Button joinRoomButton;
    private Button backButton;

    public override void _Ready()
    {
        createRoomButton = GetNode<Button>("MultiplayerOptions/CreateRoomButton");
        joinRoomButton = GetNode<Button>("MultiplayerOptions/JoinRoomButton");
        backButton = GetNode<Button>("MultiplayerOptions/BackButton");

        if (createRoomButton == null || joinRoomButton == null || backButton == null)
        {
            GD.PrintErr("Ошибка: Не найдены кнопки в MultiplayerMenu!");
            GD.Print($"createRoom: {createRoomButton}, joinRoom: {joinRoomButton}, back: {backButton}");
            return;
        }

        createRoomButton.Pressed += OnCreateRoomButtonPressed;
        joinRoomButton.Pressed += OnJoinRoomButtonPressed;
        backButton.Pressed += OnBackButtonPressed;

        GD.Print("MultiplayerMenu initialized");
    }

    private void OnCreateRoomButtonPressed()
    {
        GD.Print("Create Room button pressed!");
    }

    private void OnJoinRoomButtonPressed()
    {
        GD.Print("Join Room button pressed!");
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