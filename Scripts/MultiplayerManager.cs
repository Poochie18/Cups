using Godot;
using System;

public partial class MultiplayerManager : Node
{
    private WebSocketPeer wsPeer;
    private const string ServerUrl = "wss://tic-tac-toe-server-b4ms.onrender.com"; // Ваш URL
    private string currentRoomCode;
    private bool isHost = false;

    [Signal]
    public delegate void RoomCreatedEventHandler(string code);

    [Signal]
    public delegate void PlayerConnectedEventHandler();

    [Signal]
    public delegate void MessageReceivedEventHandler(string message);

    public override void _Ready()
    {
        wsPeer = new WebSocketPeer();
        GD.Print("MultiplayerManager initialized with WebSocketPeer");
    }

    public void CreateRoom()
    {
        if (wsPeer != null && wsPeer.GetReadyState() != WebSocketPeer.State.Closed)
        {
            wsPeer.Close();
            GD.Print("Previous peer cleared before creating new room");
        }

        wsPeer = new WebSocketPeer();
        Error error = wsPeer.ConnectToUrl($"{ServerUrl}/create");
        if (error != Error.Ok)
        {
            GD.PrintErr($"Failed to connect to {ServerUrl}/create: Error {error}");
            return;
        }

        isHost = true;
        GD.Print("Connecting to server to create room");
    }

    public void JoinRoom(string code)
    {
        if (wsPeer != null && wsPeer.GetReadyState() != WebSocketPeer.State.Closed)
        {
            wsPeer.Close();
            GD.Print("Previous peer cleared before joining room");
        }

        wsPeer = new WebSocketPeer();
        currentRoomCode = code;
        Error error = wsPeer.ConnectToUrl($"{ServerUrl}/join?room={code}");
        if (error != Error.Ok)
        {
            GD.PrintErr($"Failed to connect to {ServerUrl}/join?room={code}: Error {error}");
            return;
        }

        isHost = false;
        GD.Print($"Connecting to room {code} via {ServerUrl}/join?room={code}");
    }

    public void SendMessage(string message)
    {
        if (wsPeer != null && wsPeer.GetReadyState() == WebSocketPeer.State.Open)
        {
            wsPeer.SendText(message);
            GD.Print($"Sent message: {message}");
        }
        else
        {
            GD.PrintErr("WebSocket not connected, cannot send message");
        }
    }

    public void Cleanup()
    {
        if (wsPeer != null && wsPeer.GetReadyState() != WebSocketPeer.State.Closed)
        {
            wsPeer.Close();
            wsPeer = new WebSocketPeer();
            GD.Print("Network peer cleaned up on menu return");
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

                    // Проверяем, является ли сообщение JSON
                    if (message.StartsWith("{") && message.EndsWith("}"))
                    {
                        var json = new Json();
                        Error parseResult = json.Parse(message);
                        if (parseResult == Error.Ok)
                        {
                            Variant dataVariant = json.Data;
                            if (dataVariant.VariantType == Variant.Type.Dictionary)
                            {
                                var data = (Godot.Collections.Dictionary)dataVariant.Obj;
                                if (data.ContainsKey("type"))
                                {
                                    string type = data["type"].AsString();
                                    if (type == "created" && data.ContainsKey("roomCode"))
                                    {
                                        currentRoomCode = data["roomCode"].AsString();
                                        EmitSignal(SignalName.RoomCreated, currentRoomCode);
                                    }
                                    else if (type == "start")
                                    {
                                        EmitSignal(SignalName.PlayerConnected);
                                        GetTree().ChangeSceneToFile("res://Scenes/Game.tscn");
                                    }
                                    else if (type == "error")
                                    {
                                        GD.PrintErr($"Server error: {message}");
                                    }
                                }
                            }
                            else
                            {
                                GD.Print($"Received JSON but not a dictionary: {message}");
                                EmitSignal(SignalName.MessageReceived, message);
                            }
                        }
                        else
                        {
                            GD.PrintErr($"Failed to parse JSON: {parseResult} for message: {message}");
                            EmitSignal(SignalName.MessageReceived, message); // Передаём как строку
                        }
                    }
                    else
                    {
                        // Если не JSON, передаём как есть
                        EmitSignal(SignalName.MessageReceived, message);
                    }
                }
            }
        }
        else if (state == WebSocketPeer.State.Closed)
        {
            //GD.Print("WebSocket connection closed");
        }
    }

    public override void _ExitTree()
    {
        if (wsPeer != null && wsPeer.GetReadyState() != WebSocketPeer.State.Closed)
        {
            wsPeer.Close();
            GD.Print("Network peer closed on exit");
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