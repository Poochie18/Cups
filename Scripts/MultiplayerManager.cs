using Godot;
using System;

public partial class MultiplayerManager : Node
{
    private ENetMultiplayerPeer peer;
    private const int DEFAULT_PORT = 8910;

    [Signal]
    public delegate void RoomCreatedEventHandler(string code);

    [Signal]
    public delegate void PlayerConnectedEventHandler();

    public override void _Ready()
    {
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.ConnectedToServer += OnConnectedToServer;
        GD.Print("MultiplayerManager initialized");
    }

    public string CreateRoom()
    {
        if (peer != null)
        {
            peer.Close();
            peer = null;
            Multiplayer.MultiplayerPeer = null;
            GD.Print("Предыдущий пир очищен перед созданием нового сервера");
        }

        peer = new ENetMultiplayerPeer();
        Error error = peer.CreateServer(DEFAULT_PORT, 1);
        if (error != Error.Ok)
        {
            GD.PrintErr($"Ошибка создания сервера: {error}");
            return null;
        }
        Multiplayer.MultiplayerPeer = peer;
        string roomCode = GenerateRoomCode();
        GD.Print($"Сервер запущен. Код комнаты: {roomCode}");
        EmitSignal(SignalName.RoomCreated, roomCode);
        return roomCode;
    }

    public void JoinRoom(string code, string ip = "127.0.0.1")
    {
        if (peer != null)
        {
            peer.Close();
            peer = null;
            Multiplayer.MultiplayerPeer = null;
            GD.Print("Предыдущий пир очищен перед подключением клиента");
        }

        peer = new ENetMultiplayerPeer();
        Error error = peer.CreateClient(ip, DEFAULT_PORT);
        if (error != Error.Ok)
        {
            GD.PrintErr($"Ошибка подключения к {ip}:{DEFAULT_PORT}: {error}");
            return;
        }
        Multiplayer.MultiplayerPeer = peer;
        GD.Print($"Подключение к комнате {code}");
    }

    private string GenerateRoomCode()
    {
        Random rand = new Random();
        return rand.Next(1000, 10000).ToString();
    }

    private void OnPeerConnected(long id)
    {
        GD.Print($"Игрок подключился: {id}");
        if (Multiplayer.IsServer())
        {
            EmitSignal(SignalName.PlayerConnected);
            RpcId(0, nameof(StartGame)); // Сервер инициирует переход для всех
        }
    }

    private void OnConnectedToServer()
    {
        GD.Print("Успешно подключились к серверу");
        EmitSignal(SignalName.PlayerConnected);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void StartGame()
    {
        GD.Print($"StartGame called on ID {Multiplayer.GetUniqueId()}");
        GetTree().ChangeSceneToFile("res://Scenes/Game.tscn");
    }

    public void Cleanup()
    {
        if (peer != null)
        {
            peer.Close();
            peer = null;
            Multiplayer.MultiplayerPeer = null;
            GD.Print("Сетевой пир очищен при возвращении в меню");
        }
    }

    public override void _ExitTree()
    {
        if (peer != null)
        {
            peer.Close();
            GD.Print("Сетевой пир закрыт при выходе");
        }
    }
}