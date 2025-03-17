using Godot;

public partial class MultiplayerMenu : Control
{
    private Button createRoomButton;
    private LineEdit roomCodeInput;
    private Button joinRoomButton;
    private Button backButton;
    private Label roomCodeLabel;
    private MultiplayerManager multiplayerManager;
    private VBoxContainer multiplayerOptions;

    public override void _Ready()
    {
        multiplayerOptions = GetNode<VBoxContainer>("MultiplayerOptions");
        createRoomButton = GetNode<Button>("MultiplayerOptions/CreateRoomButton");
        roomCodeInput = GetNode<LineEdit>("MultiplayerOptions/RoomCodeInput");
        joinRoomButton = GetNode<Button>("MultiplayerOptions/JoinRoomButton");
        backButton = GetNode<Button>("MultiplayerOptions/BackButton");
        roomCodeLabel = GetNode<Label>("MultiplayerOptions/RoomCodeLabel");
        multiplayerManager = GetNode<MultiplayerManager>("/root/MultiplayerManager");

        if (multiplayerOptions == null || createRoomButton == null || roomCodeInput == null || 
            joinRoomButton == null || backButton == null || roomCodeLabel == null || multiplayerManager == null)
        {
            GD.PrintErr("Ошибка: Не найдены узлы в MultiplayerMenu!");
            return;
        }

        roomCodeLabel.MaxLinesVisible = 1;
        roomCodeLabel.ClipText = true;
        roomCodeLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        roomCodeLabel.CustomMinimumSize = new Vector2(200, 0);

        createRoomButton.SizeFlagsHorizontal = SizeFlags.Fill;
        roomCodeInput.SizeFlagsHorizontal = SizeFlags.Fill;
        joinRoomButton.SizeFlagsHorizontal = SizeFlags.Fill;
        backButton.SizeFlagsHorizontal = SizeFlags.Fill;

        createRoomButton.Pressed += OnCreateRoomButtonPressed;
        joinRoomButton.Pressed += OnJoinRoomButtonPressed;
        backButton.Pressed += OnBackButtonPressed;
        multiplayerManager.Connect("RoomCreated", Callable.From((string code) => OnRoomCreated(code)));
        multiplayerManager.Connect("PlayerConnected", Callable.From(OnPlayerConnected));

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
        else
        {
            roomCodeLabel.Text = $"Room Code: {code}";
            GD.Print($"Ожидание подключения второго игрока...");
        }
    }

    private void OnRoomCreated(string code)
    {
        GD.Print($"Room created: {code}");
        roomCodeLabel.Text = $"Room Code: {code} (Waiting...)";
    }

    private void OnJoinRoomButtonPressed()
    {
        string roomCode = roomCodeInput.Text.Trim();
        if (string.IsNullOrEmpty(roomCode))
        {
            GD.Print("Ошибка: Введите код комнаты!");
            roomCodeLabel.Text = "Enter a room code!";
            return;
        }
        GD.Print($"Join Room button pressed! Room code: {roomCode}");
        multiplayerManager.JoinRoom(roomCode);
        roomCodeLabel.Text = "Connecting...";
    }

    private void OnPlayerConnected()
    {
        GD.Print("Игрок подключен, ожидаем перехода в игру");
        QueueFree(); // Удаляем меню, ждем RPC StartGame
    }

    private void OnBackButtonPressed()
    {
        GD.Print("Back button pressed!");
        multiplayerManager.Cleanup();
        GetTree().ChangeSceneToFile("res://Scenes/Menu.tscn");
        QueueFree();
    }
}