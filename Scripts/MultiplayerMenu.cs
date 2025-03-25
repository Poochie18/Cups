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
        multiplayerOptions = GetNode<VBoxContainer>("Frame/MultiplayerOptions");
        createRoomButton = GetNode<Button>("Frame/MultiplayerOptions/CreateRoomButton");
        roomCodeInput = GetNode<LineEdit>("Frame/MultiplayerOptions/RoomCodeInput");
        joinRoomButton = GetNode<Button>("Frame/MultiplayerOptions/JoinRoomButton");
        backButton = GetNode<Button>("Frame/MultiplayerOptions/BackButton");
        roomCodeLabel = GetNode<Label>("Frame/MultiplayerOptions/RoomCodeLabel");
        multiplayerManager = GetNode<MultiplayerManager>("/root/MultiplayerManager");

        if (!ValidateNodes()) return;

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
    }

    private bool ValidateNodes()
    {
        if (multiplayerOptions == null || createRoomButton == null || roomCodeInput == null || 
            joinRoomButton == null || backButton == null || roomCodeLabel == null || multiplayerManager == null)
        {
            GD.PrintErr("Ошибка: Не найдены узлы в MultiplayerMenu!");
            return false;
        }
        return true;
    }

    private void OnCreateRoomButtonPressed()
    {
        multiplayerManager.CreateRoom();
        roomCodeLabel.Text = "Creating room...";
        roomCodeLabel.Modulate = new Color(1, 1, 1);
    }

    private void OnRoomCreated(string code)
    {
        roomCodeLabel.Text = $"Room Code: {code} (Waiting...)";
        roomCodeLabel.Modulate = new Color(1, 1, 1);
    }

    private void OnJoinRoomButtonPressed()
    {
        string roomCode = roomCodeInput.Text.Trim().ToUpper();
        if (string.IsNullOrEmpty(roomCode))
        {
            roomCodeLabel.Text = "Enter a room code!";
            roomCodeLabel.Modulate = new Color(1, 0, 0);
            return;
        }

        if (multiplayerManager.IsHost())
        {
            string currentRoomCode = multiplayerManager.GetRoomCode();
            if (string.IsNullOrEmpty(currentRoomCode))
            {
                roomCodeLabel.Text = "Room not created yet!";
                roomCodeLabel.Modulate = new Color(1, 0, 0);
                return;
            }

            if (roomCode == currentRoomCode)
            {
                roomCodeLabel.Text = "Вы не можете присоединиться к своей комнате!";
                roomCodeLabel.Modulate = new Color(1, 0, 0);
                return;
            }
        }

        multiplayerManager.JoinRoom(roomCode);
        roomCodeLabel.Text = "Connecting...";
        roomCodeLabel.Modulate = new Color(1, 1, 1);
    }

    private void OnPlayerConnected()
    {
        QueueFree();
    }

    private void OnBackButtonPressed()
    {
        multiplayerManager.Cleanup();
        GetTree().ChangeSceneToFile("res://Scenes/Menu.tscn");
        QueueFree();
    }
}