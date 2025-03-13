using Godot;

public partial class MultiplayerMenu : Control
{
    private Button createRoomButton;
    private LineEdit roomCodeInput;
    private Button joinRoomButton;
    private Button backButton;
    private Label roomCodeLabel;
    private MultiplayerManager multiplayerManager;

    public override void _Ready()
    {
        createRoomButton = GetNode<Button>("MultiplayerOptions/CreateRoomButton");
        roomCodeInput = GetNode<LineEdit>("MultiplayerOptions/RoomCodeInput");
        joinRoomButton = GetNode<Button>("MultiplayerOptions/JoinRoomButton");
        backButton = GetNode<Button>("MultiplayerOptions/BackButton");
        roomCodeLabel = GetNode<Label>("MultiplayerOptions/RoomCodeLabel");
        multiplayerManager = GetNode<MultiplayerManager>("MultiplayerManager");

        if (createRoomButton == null || roomCodeInput == null || joinRoomButton == null || 
            backButton == null || roomCodeLabel == null || multiplayerManager == null)
        {
            GD.PrintErr("Ошибка: Не найдены узлы в MultiplayerMenu!");
            GD.Print($"createRoom: {createRoomButton}, roomCode: {roomCodeInput}, joinRoom: {joinRoomButton}");
            GD.Print($"back: {backButton}, roomCodeLabel: {roomCodeLabel}, multiplayerManager: {multiplayerManager}");
            return;
        }

        createRoomButton.Pressed += OnCreateRoomButtonPressed;
        joinRoomButton.Pressed += OnJoinRoomButtonPressed;
        backButton.Pressed += OnBackButtonPressed;
        multiplayerManager.Connect("RoomCreated", Callable.From((string code) => OnRoomCreated(code))); // Исправлено

        GD.Print("MultiplayerMenu initialized");
    }

    private void OnCreateRoomButtonPressed()
    {
        GD.Print("Create Room button pressed!");
        string code = multiplayerManager.CreateRoom();
        if (code == null)
        {
            roomCodeLabel.Text = "Failed to create room!";
        }
    }

    private void OnRoomCreated(string code)
    {
        roomCodeLabel.Text = $"Room Code: {code}";
        GD.Print($"Room code updated in UI: {code}");
    }

    private void OnJoinRoomButtonPressed()
    {
        string roomCode = roomCodeInput.Text.Trim();
        if (string.IsNullOrEmpty(roomCode))
        {
            GD.Print("Ошибка: Введите код комнаты!");
            return;
        }
        GD.Print($"Join Room button pressed! Room code: {roomCode}");
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