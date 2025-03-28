using Godot;

public partial class MultiplayerMenu : Control
{
    private Button createRoomButton;
    private LineEdit roomCodeInput;
    private Button joinRoomButton;
    private Button backButton;
    private Label roomCodeLabel;
    private MultiplayerManager multiplayerManager;
    private Timer searchTimeoutTimer;
    private bool isSearching;

    public override void _Ready()
    {
        //GD.Print("MultiplayerMenu: Entering _Ready");

        createRoomButton = GetNode<Button>("MultiplayerCreate/CreateRoomButton");
        joinRoomButton = GetNode<Button>("MultiplayerJoin/JoinRoomButton");
        roomCodeInput = GetNode<LineEdit>("MultiplayerJoin/RoomCodeInput");
        roomCodeLabel = GetNode<Label>("MultiplayerCreate/RoomCodeLabel");
        backButton = GetNode<Button>("BackButton");
        multiplayerManager = GetNode<MultiplayerManager>("/root/MultiplayerManager");

        searchTimeoutTimer = new Timer();
        searchTimeoutTimer.WaitTime = 5.0f;
        searchTimeoutTimer.OneShot = true;
        AddChild(searchTimeoutTimer);
        searchTimeoutTimer.Timeout += OnSearchTimeout;

        if (!ValidateNodes()) return;

        //GD.Print("MultiplayerMenu: All nodes found");

        //GD.Print($"CreateRoomButton - Visible: {createRoomButton.Visible}, Disabled: {createRoomButton.Disabled}, MouseFilter: {createRoomButton.MouseFilter}");
        //GD.Print($"JoinRoomButton - Visible: {joinRoomButton.Visible}, Disabled: {joinRoomButton.Disabled}, MouseFilter: {joinRoomButton.MouseFilter}");
        //GD.Print($"BackButton - Visible: {backButton.Visible}, Disabled: {backButton.Disabled}, MouseFilter: {backButton.MouseFilter}");

        createRoomButton.SizeFlagsHorizontal = SizeFlags.Fill;
        roomCodeInput.SizeFlagsHorizontal = SizeFlags.Fill;
        joinRoomButton.SizeFlagsHorizontal = SizeFlags.Fill;
        backButton.SizeFlagsHorizontal = SizeFlags.Fill;

        createRoomButton.Pressed += () =>
        {
            //GD.Print("CreateRoomButton pressed");
            OnCreateRoomButtonPressed();
        };
        joinRoomButton.Pressed += () =>
        {
            //GD.Print("JoinRoomButton pressed");
            OnJoinRoomButtonPressed();
        };
        backButton.Pressed += () =>
        {
            //GD.Print("BackButton pressed");
            OnBackButtonPressed();
        };
        multiplayerManager.Connect("RoomCreated", Callable.From((string code) => OnRoomCreated(code)));
        multiplayerManager.Connect("PlayerConnected", Callable.From(OnPlayerConnected));

        //GD.Print("MultiplayerMenu initialized");
    }

    private bool ValidateNodes()
    {
        if (createRoomButton == null || roomCodeInput == null || 
            joinRoomButton == null || backButton == null || roomCodeLabel == null || multiplayerManager == null)
        {
            //GD.PrintErr("Error: Nodes not found in MultiplayerMenu!");
            //GD.Print($"createRoomButton: {createRoomButton}, roomCodeInput: {roomCodeInput}, joinRoomButton: {joinRoomButton}, backButton: {backButton}, roomCodeLabel: {roomCodeLabel}, multiplayerManager: {multiplayerManager}");
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
        if (code.StartsWith("ERROR:"))
        {
            roomCodeLabel.Text = code;
            if (isSearching)
            {
                StopSearch("Connection error: " + code);
            }
        }
        else
        {
            roomCodeLabel.Text = $"Code: {code} (Waiting...)";
            roomCodeLabel.Modulate = new Color(1, 1, 1);
        }
    }

    private void OnJoinRoomButtonPressed()
    {
        string roomCode = roomCodeInput.Text.Trim().ToUpper();
        if (string.IsNullOrEmpty(roomCode))
        {
            roomCodeInput.PlaceholderText = "Enter code!";
            return;
        }

        if (multiplayerManager.IsHost())
        {
            string currentRoomCode = multiplayerManager.GetRoomCode();
            if (string.IsNullOrEmpty(currentRoomCode))
            {
                roomCodeInput.Text = "";
                roomCodeInput.PlaceholderText = "Room not created!";
                return;
            }
            if (roomCode == currentRoomCode)
            {
                roomCodeInput.Text = "";
                roomCodeInput.PlaceholderText = "This is your room!";
                return;
            }
        }

        isSearching = true;
        joinRoomButton.Text = "Connection";
        joinRoomButton.Disabled = true;
        roomCodeInput.PlaceholderText = "Connecting...";
        roomCodeInput.Modulate = new Color(1, 1, 1);

        searchTimeoutTimer.Start();
        //GD.Print("Search timeout timer started (15 seconds)");

        multiplayerManager.JoinRoom(roomCode);
    }

    private void OnSearchTimeout()
    {
        //GD.Print("Search timeout: 15 seconds elapsed, room not found");
        StopSearch("Room not found!");
    }

    private void StopSearch(string errorMessage)
    {
        if (!isSearching) return;

        isSearching = false;
        searchTimeoutTimer.Stop();
        joinRoomButton.Text = "Join Room";
        joinRoomButton.Disabled = false;
        roomCodeInput.Text = "";
        roomCodeInput.PlaceholderText = "Room not found";
        roomCodeInput.Modulate = new Color(1, 1, 1);

        multiplayerManager.Cleanup();
        //GD.Print("Search stopped: " + errorMessage);
    }

    private void OnPlayerConnected()
    {
        if (isSearching)
        {
            searchTimeoutTimer.Stop();
            isSearching = false;
            //GD.Print("Search stopped due to successful connection");
        }

        //GD.Print("Player connected, closing MultiplayerMenu");
        var error = GetTree().ChangeSceneToFile("res://Scenes/Game.tscn");
        if (error != Error.Ok)
        {
            //GD.PrintErr($"Failed to load scene Game.tscn: Error {error}");
        }
        QueueFree();
    }

    private void OnBackButtonPressed()
    {
        if (isSearching)
        {
            StopSearch("Search canceled");
        }

        multiplayerManager.Cleanup();
        var error = GetTree().ChangeSceneToFile("res://Scenes/Menu.tscn");
        if (error != Error.Ok)
        {
            //GD.PrintErr($"Failed to load scene Menu.tscn: Error {error}");
        }
        QueueFree();
    }

    public override void _ExitTree()
    {
        if (searchTimeoutTimer != null)
        {
            searchTimeoutTimer.Stop();
            searchTimeoutTimer.QueueFree();
            //GD.Print("Search timeout timer cleaned up on exit");
        }
    }
}