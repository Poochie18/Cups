using Godot;
using System;

public partial class MultiplayerManager : Node
{
    private WebSocketPeer wsPeer;
    private const string ServerBaseUrl = "wss://tic-tac-toe-server-new.onrender.com";
    private string currentRoomCode = "";
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
        //GD.Print("MultiplayerManager initialized with WebSocketPeer");
    }

    public void CreateRoom()
    {
        if (wsPeer != null && wsPeer.GetReadyState() != WebSocketPeer.State.Closed)
        {
            wsPeer.Close();
            //GD.Print("Previous peer cleared before creating new room");
        }

        wsPeer = new WebSocketPeer();
        string url = $"{ServerBaseUrl}/create";
        Error error = wsPeer.ConnectToUrl(url);
        if (error != Error.Ok)
        {
            //GD.PrintErr($"Failed to connect to {url}: Error {error}");
            EmitSignal(SignalName.RoomCreated, "ERROR: Failed to connect");
            return;
        }

        isHost = true;
        currentRoomCode = "";
        //GD.Print($"Connecting to server to create room at {url}");
    }

    public void JoinRoom(string code)
    {
        if (wsPeer != null && wsPeer.GetReadyState() != WebSocketPeer.State.Closed)
        {
            wsPeer.Close();
            //GD.Print("Previous peer cleared before joining room");
        }

        wsPeer = new WebSocketPeer();
        currentRoomCode = code;
        string url = $"{ServerBaseUrl}/join?room={code}";
        Error error = wsPeer.ConnectToUrl(url);
        if (error != Error.Ok)
        {
            //GD.PrintErr($"Failed to connect to {url}: Error {error}");
            EmitSignal(SignalName.RoomCreated, "ERROR: Failed to connect");
            return;
        }

        isHost = false;
        //GD.Print($"Connecting to room {code} via {url}");
    }

    public void SendMessage(string message)
    {
        if (wsPeer != null && wsPeer.GetReadyState() == WebSocketPeer.State.Open)
        {
            wsPeer.SendText(message);
            //GD.Print($"Sent message: {message}");
        }
        else
        {
            //GD.PrintErr("WebSocket not connected, cannot send message");
        }
    }

    public void Cleanup()
    {
        if (wsPeer != null && wsPeer.GetReadyState() != WebSocketPeer.State.Closed)
        {
            wsPeer.Close();
            wsPeer = new WebSocketPeer();
            //GD.Print("Network peer cleaned up on menu return");
        }
        currentRoomCode = "";
        isHost = false;
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
                    //GD.Print($"MultiplayerManager received: {message}");

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
                                        string errorMessage = data.ContainsKey("message") ? data["message"].AsString() : "Unknown error";
                                        //GD.PrintErr($"Server error: {errorMessage}");
                                        EmitSignal(SignalName.RoomCreated, $"ERROR: {errorMessage}");
                                    }
                                }
                            }
                            else
                            {
                                //GD.Print($"Received JSON but not a dictionary: {message}");
                                EmitSignal(SignalName.MessageReceived, message);
                            }
                        }
                        else
                        {
                            //GD.PrintErr($"Failed to parse JSON: {parseResult} for message: {message}");
                            EmitSignal(SignalName.MessageReceived, message);
                        }
                    }
                    else
                    {
                        EmitSignal(SignalName.MessageReceived, message);
                    }
                }
            }
        }
        else if (state == WebSocketPeer.State.Closed)
        {
            var code = wsPeer.GetCloseCode();
            var reason = wsPeer.GetCloseReason();
            //GD.Print($"WebSocket closed with code: {code}, reason: {reason}");
        }
    }

    public override void _ExitTree()
    {
        if (wsPeer != null && wsPeer.GetReadyState() != WebSocketPeer.State.Closed)
        {
            wsPeer.Close();
            //GD.Print("Network peer closed on exit");
        }
    }

    public bool IsHost()
    {
        return isHost;
    }

    public string GetRoomCode()
    {
        return currentRoomCode;
    }

    public WebSocketPeer GetWebSocketPeer()
    {
        return wsPeer;
    }
}