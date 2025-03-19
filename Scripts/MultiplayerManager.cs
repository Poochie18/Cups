using Godot;
using System;

public partial class MultiplayerManager : Node
{
    private WebSocketPeer wsPeer;
    private const string ServerUrl = "ws://localhost:8080";
    private string currentRoomCode;
    private bool isHost = false;

    [Signal]
    public delegate void RoomCreatedEventHandler(string code);

    [Signal]
    public delegate void PlayerConnectedEventHandler();

    [Signal]
    public delegate void MessageReceivedEventHandler(string message); // Новый сигнал для сообщений

    public override void _Ready()
    {
        wsPeer = new WebSocketPeer();
        GD.Print("MultiplayerManager initialized with WebSocketPeer");
    }

    public string CreateRoom()
    {
        if (wsPeer != null && wsPeer.GetReadyState() != WebSocketPeer.State.Closed)
        {
            wsPeer.Close();
            GD.Print("Предыдущий пир очищен перед созданием новой комнаты");
        }

        wsPeer = new WebSocketPeer();
        currentRoomCode = GenerateRoomCode();
        string url = $"{ServerUrl}/create?room={currentRoomCode}";
        Error error = wsPeer.ConnectToUrl(url);
        if (error != Error.Ok)
        {
            GD.PrintErr($"Ошибка создания комнаты {currentRoomCode}: {error}");
            return null;
        }

        isHost = true;
        GD.Print($"Подключение к серверу для создания комнаты: {currentRoomCode}");
        EmitSignal(SignalName.RoomCreated, currentRoomCode);
        return currentRoomCode;
    }

    public void JoinRoom(string code, string ip = "")
    {
        if (wsPeer != null && wsPeer.GetReadyState() != WebSocketPeer.State.Closed)
        {
            wsPeer.Close();
            GD.Print("Предыдущий пир очищен перед подключением клиента");
        }

        wsPeer = new WebSocketPeer();
        currentRoomCode = code;
        string url = $"{ServerUrl}/join?room={code}";
        Error error = wsPeer.ConnectToUrl(url);
        if (error != Error.Ok)
        {
            GD.PrintErr($"Ошибка подключения к комнате {code}: {error}");
            return;
        }

        isHost = false;
        GD.Print($"Подключение к комнате {code} через {url}");
    }

    private string GenerateRoomCode()
    {
        Random rand = new Random();
        return rand.Next(1000, 10000).ToString();
    }

    public void Cleanup()
    {
        if (wsPeer != null && wsPeer.GetReadyState() != WebSocketPeer.State.Closed)
        {
            wsPeer.Close();
            wsPeer = new WebSocketPeer();
            GD.Print("Сетевой пир очищен при возвращении в меню");
        }
    }

    public override void _Process(double delta)
    {
        if (wsPeer == null) return;

        wsPeer.Poll();
        var state = wsPeer.GetReadyState();
        if (state == WebSocketPeer.State.Open)
        {
            while (wsPeer.GetAvailablePacketCount() > 0)
            {
                var packet = wsPeer.GetPacket();
                if (packet != null && !packet.IsEmpty())
                {
                    string message = packet.GetStringFromUtf8();
                    GD.Print($"MultiplayerManager received: {message}");
                    if (message.Contains("\"type\":\"start\""))
                    {
                        EmitSignal(SignalName.PlayerConnected);
                        GetTree().ChangeSceneToFile("res://Scenes/Game.tscn");
                    }
                    else
                    {
                        EmitSignal(SignalName.MessageReceived, message); // Передаём все остальные сообщения
                    }
                }
            }
        }
        else if (state == WebSocketPeer.State.Closed)
        {
            //GD.Print("WebSocket соединение закрыто");
        }
    }

    public override void _ExitTree()
    {
        if (wsPeer != null && wsPeer.GetReadyState() != WebSocketPeer.State.Closed)
        {
            wsPeer.Close();
            GD.Print("Сетевой пир закрыт при выходе");
        }
    }

    public bool IsHost()
    {
        return isHost;
    }

    public WebSocketPeer GetWebSocketPeer()
    {
        return wsPeer;
    }
}