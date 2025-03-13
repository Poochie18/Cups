using Godot;
using System;

public partial class MultiplayerManager : Node
{
    private ENetMultiplayerPeer peer;
    private string roomCode;
    private const int DEFAULT_PORT = 8910;

    [Signal]
    public delegate void RoomCreatedEventHandler(string code);

    [Signal]
    public delegate void ConnectionFailedEventHandler();

    public override void _Ready()
    {
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.ConnectionFailed += OnConnectionFailed;
        GD.Print("MultiplayerManager initialized");
    }

    public string CreateRoom()
    {
        peer = new ENetMultiplayerPeer();
        Error error = peer.CreateServer(DEFAULT_PORT, 2); // Максимум 2 игрока
        if (error != Error.Ok)
        {
            GD.PrintErr($"Ошибка создания сервера: {error}");
            return null;
        }

        Multiplayer.MultiplayerPeer = peer;
        roomCode = GenerateRoomCode();
        GD.Print($"Сервер запущен. Код комнаты: {roomCode}");
        EmitSignal(SignalName.RoomCreated, roomCode);
        return roomCode;
    }

    private string GenerateRoomCode()
    {
        Random rand = new Random();
        return rand.Next(1000, 10000).ToString(); // Случайный 4-значный код
    }

    private void OnPeerConnected(long id)
    {
        GD.Print($"Игрок подключился: {id}");
    }

    private void OnPeerDisconnected(long id)
    {
        GD.Print($"Игрок отключился: {id}");
    }

    private void OnConnectedToServer()
    {
        GD.Print("Успешно подключились к серверу");
    }

    private void OnConnectionFailed()
    {
        GD.Print("Не удалось подключиться к серверу");
        EmitSignal(SignalName.ConnectionFailed);
    }

    public override void _ExitTree()
    {
        if (peer != null)
        {
            peer.Close();
            GD.Print("Сетевой пир закрыт");
        }
    }
}