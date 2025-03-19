using Godot;
using System.Collections.Generic;

public partial class Game : Node2D
{
    private string[,] board = new string[3, 3];
    private string currentPlayer = "Player1";
    private GridContainer grid;
    private Control player1Table;
    private Control player2Table;
    private Label player1Label;
    private Label player2Label;
    private UI ui;
    private bool gameEnded = false;
    private TextureRect draggedCircle = null;
    private Vector2 dragOffset = Vector2.Zero;
    private Dictionary<string, Vector2> initialPositions = new Dictionary<string, Vector2>();
    private Dictionary<string, Node> initialParents = new Dictionary<string, Node>();
    private Button highlightedButton = null;
    private Button[] gridButtons;
    public string gameMode = "multiplayer";
    private Button backToMenuButton;
    private Button restartButton;
    private MultiplayerManager multiplayerManager;

    private static readonly int[,] WinningCombinations = new int[,]
    {
        {0, 1, 2}, {3, 4, 5}, {6, 7, 8},
        {0, 3, 6}, {1, 4, 7}, {2, 5, 8},
        {0, 4, 8}, {2, 4, 6}
    };

    public override void _Ready()
    {
        GD.Print($"Game scene loaded");
        grid = GetNode<GridContainer>("Grid");
        player1Table = GetNode<Control>("Player1Table");
        player2Table = GetNode<Control>("Player2Table");
        player1Label = GetNode<Label>("Player1Table/Player1Label");
        player2Label = GetNode<Label>("Player2Table/Player2Label");
        ui = GetNode<UI>("UI");
        backToMenuButton = GetNode<Button>("UI/BackToMenuButton");
        restartButton = GetNode<Button>("UI/RestartButton");
        multiplayerManager = GetNode<MultiplayerManager>("/root/MultiplayerManager");

        if (grid == null || player1Table == null || player2Table == null || 
            player1Label == null || player2Label == null || ui == null || 
            backToMenuButton == null || restartButton == null || multiplayerManager == null)
        {
            GD.PrintErr("Ошибка: Один из узлов не найден!");
            GD.Print($"grid: {grid}, player1Table: {player1Table}, player2Table: {player2Table}, " +
                    $"player1Label: {player1Label}, player2Label: {player2Label}, ui: {ui}, " +
                    $"backToMenuButton: {backToMenuButton}, restartButton: {restartButton}, " +
                    $"multiplayerManager: {multiplayerManager}");
            return;
        }

        grid.Columns = 3;
        gridButtons = new Button[9];
        for (int i = 0; i < 9; i++)
        {
            Button button = new Button
            {
                Name = "Button" + i,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                Disabled = true
            };
            button.AddThemeFontSizeOverride("font_size", CalculateFontSize());
            grid.AddChild(button);
            gridButtons[i] = button;
        }

        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                board[i, j] = "";

        CreateCircles(player1Table, "P1");
        CreateCircles(player2Table, "P2");

        var global = GetNode<Global>("/root/Global");
        if (multiplayerManager.IsHost())
        {
            currentPlayer = "Player1";
            player1Label.Text = global.PlayerNickname;
            player2Label.Text = global.OpponentNickname;
            ui.UpdateStatus($"Ход {global.PlayerNickname}");
            SendMessage($"sync_labels:{global.PlayerNickname}");
        }
        else
        {
            currentPlayer = "Player2";
            player1Label.Text = global.OpponentNickname;
            player2Label.Text = global.PlayerNickname;
            ui.UpdateStatus($"Ход {global.OpponentNickname}");

            // Перестановка позиций столов
            Vector2 tempPos = player1Table.Position;
            player1Table.Position = player2Table.Position; // player1Table (P1_*) идёт вверх
            player2Table.Position = tempPos;               // player2Table (P2_*) идёт вниз

            // Круги остаются в своих столах, просто обновляем их позиции
            UpdateCirclePositions(player1Table);
            UpdateCirclePositions(player2Table);

            SendMessage($"sync_labels:{global.PlayerNickname}");
        }

        multiplayerManager.Connect("MessageReceived", Callable.From((string message) => ProcessMessage(message)));
        GD.Print($"Initial state: Host={multiplayerManager.IsHost()}, CurrentPlayer={currentPlayer}");
        backToMenuButton.Pressed += ui.OnMenuButtonPressed;
        restartButton.Pressed += ui.OnRestartButtonPressed;
    }

    private void UpdateCirclePositions(Control table)
    {
        Vector2[] sizes = { new Vector2(50, 50), new Vector2(75, 75), new Vector2(100, 100) };
        string[] sizeNames = { "Small", "Medium", "Large" };
        float tableWidth = table.Size.X;
        float totalWidth = sizes[0].X * 2 + sizes[1].X * 2 + sizes[2].X * 2;
        float spacing = (tableWidth - totalWidth) / 7f;
        float currentX = spacing;

        int smallCount = 0, mediumCount = 0, largeCount = 0;
        foreach (Node child in table.GetChildren())
        {
            if (child is TextureRect circle)
            {
                if (circle.Name.ToString().Contains("Small"))
                {
                    circle.Position = new Vector2(currentX, (table.Size.Y - sizes[0].Y) / 2);
                    currentX += sizes[0].X + spacing;
                    smallCount++;
                }
                else if (circle.Name.ToString().Contains("Medium"))
                {
                    circle.Position = new Vector2(currentX, (table.Size.Y - sizes[1].Y) / 2);
                    currentX += sizes[1].X + spacing;
                    mediumCount++;
                }
                else if (circle.Name.ToString().Contains("Large"))
                {
                    circle.Position = new Vector2(currentX, (table.Size.Y - sizes[2].Y) / 2);
                    currentX += sizes[2].X + spacing;
                    largeCount++;
                }
                GD.Print($"Updated {circle.Name} position to {circle.Position} in {table.Name}");
            }
        }
    }
    public override void _Process(double delta)
    {
        // Убираем обработку WebSocket здесь, так как она теперь в MultiplayerManager
    }

    private void ProcessMessage(string message)
    {
        GD.Print($"Processing message: '{message}'");
        var global = GetNode<Global>("/root/Global");

        if (message.StartsWith("sync_labels:"))
        {
            string senderNickname = message.Split(':')[1];
            if (multiplayerManager.IsHost())
            {
                if (global.OpponentNickname != senderNickname) // Проверяем, чтобы не перезаписывать
                {
                    global.OpponentNickname = senderNickname;
                    player2Label.Text = senderNickname;
                    SendMessage($"update_labels:{global.PlayerNickname}:{senderNickname}");
                    GD.Print($"Host set P2 label to {senderNickname}");
                }
            }
            else
            {
                if (global.OpponentNickname != senderNickname)
                {
                    global.OpponentNickname = senderNickname;
                    player1Label.Text = senderNickname;
                    SendMessage($"update_labels:{senderNickname}:{global.PlayerNickname}");
                    GD.Print($"Client set P1 label to {senderNickname}");
                }
            }
        }
        else if (message.StartsWith("update_labels:"))
        {
            var parts = message.Split(':');
            if (parts.Length != 3)
            {
                GD.Print($"Invalid update_labels format: {message}");
                return;
            }
            string serverNickname = parts[1];
            string clientNickname = parts[2];

            if (multiplayerManager.IsHost())
            {
                // Хост: P1 — свой ник, P2 — ник клиента
                player1Label.Text = global.PlayerNickname; // Poochie22
                player2Label.Text = clientNickname; // Poochie
                global.OpponentNickname = clientNickname;
            }
            else
            {
                // Клиент: P1 — ник хоста, P2 — свой ник
                player1Label.Text = serverNickname; // Poochie22
                player2Label.Text = global.PlayerNickname; // Poochie
                global.OpponentNickname = serverNickname;
            }
            GD.Print($"Labels updated: P1={player1Label.Text}, P2={player2Label.Text}");
        }
        else if (message.StartsWith("move:"))
        {
            var parts = message.Split(':');
            if (parts.Length != 8)
            {
                GD.Print($"Invalid move message format: {message}");
                return;
            }
            string circleName = parts[1];
            int row = int.Parse(parts[2]);
            int col = int.Parse(parts[3]);
            Color color = new Color(parts[4]);
            Vector2 size = new Vector2(float.Parse(parts[5]), float.Parse(parts[6]));
            string newPlayer = parts[7];
            GD.Print($"Parsed move: {circleName} to ({row}, {col}), color={color}, size={size}, newPlayer={newPlayer}");
            SyncMove(circleName, row, col, color, size);
            SyncCurrentPlayer(newPlayer);
        }
        else if (message.StartsWith("game_over:"))
        {
            GD.Print($"Received game_over message: {message}");
            var parts = message.Split(':');
            if (parts.Length < 2)
            {
                GD.Print($"Invalid game_over message format: {message}");
                return;
            }
            string result = parts[1];
            if (result == "win" && parts.Length == 3)
            {
                string winner = parts[2];
                string winnerName = winner == "Player1" ? player1Label.Text : player2Label.Text;
                GD.Print($"{winner} wins (received)!");
                ui.UpdateStatus($"{winnerName} победил!");
                gameEnded = true;
                GD.Print($"Game ended set to true for winner: {winner}");
            }
            else if (result == "draw")
            {
                GD.Print("Draw (received)!");
                ui.UpdateStatus("Ничья!");
                gameEnded = true;
                GD.Print("Game ended set to true for draw");
            }
            else
            {
                GD.Print($"Unknown game_over result: {result}");
            }
        }
        else
        {
            GD.Print($"Unknown message type: {message}");
        }
    }

    private void SyncMove(string circleName, int row, int col, Color color, Vector2 size)
    {
        if (gameEnded)
        {
            GD.Print($"SyncMove blocked: game has ended, attempted move: {circleName} to ({row}, {col})");
            return;
        }

        GD.Print($"SyncMove called: {circleName} to ({row}, {col})");

        TextureRect circle = FindCircleByName(circleName);
        if (circle == null)
        {
            Control table = circleName.StartsWith("P1") ? player1Table : player2Table;
            circle = table.GetNodeOrNull<TextureRect>(circleName);
            if (circle == null)
            {
                circle = new TextureRect
                {
                    Name = circleName,
                    Texture = GD.Load<Texture2D>("res://Sprites/icon.svg") ?? GD.Load<Texture2D>("res://icon.png"),
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    Size = size,
                    Modulate = color,
                    MouseFilter = Control.MouseFilterEnum.Stop
                };
                AddChild(circle);
                GD.Print($"Created new circle {circleName} for sync");
            }
            else
            {
                circle.GetParent().RemoveChild(circle);
                AddChild(circle);
                GD.Print($"Moved existing circle {circleName} from {table.Name}");
            }
        }
        else
        {
            GD.Print($"Found existing circle {circleName} in scene");
        }

        string existingCircleName = board[row, col];
        if (existingCircleName != "" && existingCircleName != circleName)
        {
            TextureRect existingCircle = FindCircleByName(existingCircleName);
            if (existingCircle != null)
            {
                existingCircle.GetParent().RemoveChild(existingCircle);
                existingCircle.QueueFree();
                GD.Print($"Synced removal of {existingCircleName} from ({row}, {col})");
            }
        }

        Vector2 gridPos = grid.GlobalPosition;
        Vector2 cellSize = new Vector2(grid.Size.X / 3, grid.Size.Y / 3);
        if (circle.GetParent() != this)
        {
            circle.GetParent().RemoveChild(circle);
            AddChild(circle);
        }
        circle.Position = gridPos + new Vector2(col * cellSize.X, row * cellSize.Y) + (cellSize - circle.Size) / 2;
        board[row, col] = circleName;
        GD.Print($"Placed {circleName} at position {circle.Position} on grid");

        CheckForWinOrDraw();
    }

    private void SendMessage(string message)
    {
        var wsPeer = multiplayerManager.GetWebSocketPeer();
        if (wsPeer != null && wsPeer.GetReadyState() == WebSocketPeer.State.Open)
        {
            wsPeer.SendText(message);
            GD.Print($"Sent message: {message}");
        }
        else
        {
            GD.PrintErr($"Failed to send message: {message}, WebSocket state: {wsPeer?.GetReadyState()}");
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void SyncPlayerLabels(string senderNickname)
    {
        if (player1Label == null || player2Label == null)
        {
            GD.PrintErr("Ошибка: Метки игроков не инициализированы в SyncPlayerLabels!");
            return;
        }

        var global = GetNode<Global>("/root/Global");
        if (Multiplayer.IsServer())
        {
            global.OpponentNickname = senderNickname;
            player2Label.Text = senderNickname;
            RpcId(0, nameof(UpdateOpponentLabels), global.PlayerNickname, senderNickname);
            GD.Print($"Server updated: P2Label={player2Label.Text}");
        }
        else
        {
            global.OpponentNickname = senderNickname;
            player1Label.Text = senderNickname;
            GD.Print($"Client updated: P1Label={player1Label.Text}");
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void UpdateOpponentLabels(string serverNickname, string clientNickname)
    {
        var global = GetNode<Global>("/root/Global");
        global.OpponentNickname = Multiplayer.IsServer() ? clientNickname : serverNickname;
        player1Label.Text = Multiplayer.IsServer() ? global.PlayerNickname : serverNickname;
        player2Label.Text = Multiplayer.IsServer() ? clientNickname : global.PlayerNickname;
        GD.Print($"Labels updated: P1={player1Label.Text}, P2={player2Label.Text}");
    }

    // Обработка отключения игрока
    private void OnPeerDisconnected(long id)
    {
        GD.Print($"Игрок с ID {id} отключился!");
        gameEnded = true; // Останавливаем игру
        ui.UpdateStatus("Соперник отключился. Игра окончена.");
        RpcId(0, nameof(ShowDisconnectMessage)); // Уведомляем всех игроков
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void ShowDisconnectMessage()
    {
        gameEnded = true;
        ui.UpdateStatus("Соперник отключился. Игра окончена.");
        backToMenuButton.Disabled = false; // Активируем кнопку "Back to Menu"
    }

    public override void _Input(InputEvent @event)
    {
        if (gameEnded) return;

        bool isMyTurn = (multiplayerManager.IsHost() && currentPlayer == "Player1") || 
                        (!multiplayerManager.IsHost() && currentPlayer == "Player2");
        if (!isMyTurn)
        {
            //GD.Print($"Not my turn: Host={multiplayerManager.IsHost()}, CurrentPlayer={currentPlayer}");
            return;
        }

        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            if (mouseEvent.Pressed)
                StartDragging(mouseEvent.Position);
            else if (draggedCircle != null)
            {
                ClearHighlight();
                DropCircle(mouseEvent.Position);
                draggedCircle = null;
                dragOffset = Vector2.Zero;
            }
        }
        else if (@event is InputEventMouseMotion mouseMotion && draggedCircle != null)
        {
            draggedCircle.Position = mouseMotion.Position - dragOffset;
            UpdateHighlight(mouseMotion.Position);
        }
    }

    private void StartDragging(Vector2 mousePos)
    {
        Control table = multiplayerManager.IsHost() ? player1Table : player2Table; // Хост: P1 сверху, Клиент: P2 снизу
        string prefix = multiplayerManager.IsHost() ? "P1" : "P2";

        GD.Print($"StartDragging for {currentPlayer}, Host={multiplayerManager.IsHost()}, Table={table.Name}, Prefix={prefix}, MousePos={mousePos}");

        bool foundCircle = false;
        foreach (Node child in table.GetChildren())
        {
            TextureRect circle = child as TextureRect;
            if (circle != null)
            {
                Rect2 circleRect = new Rect2(circle.GlobalPosition, circle.Size);
                GD.Print($"Checking circle {circle.Name} at {circle.GlobalPosition}, Size={circle.Size}, Rect={circleRect}");
                if (circle.Name.ToString().StartsWith(prefix) && circleRect.HasPoint(mousePos))
                {
                    draggedCircle = circle;
                    dragOffset = mousePos - circle.GlobalPosition;
                    circle.GetParent().RemoveChild(circle);
                    AddChild(circle);
                    circle.Position = mousePos - dragOffset;
                    GD.Print($"Started dragging {circle.Name} from {circle.Position} by {currentPlayer}");
                    foundCircle = true;
                    break;
                }
            }
        }

        if (!foundCircle)
        {
            GD.Print($"No circle found in {table.Name} matching prefix {prefix} at MousePos={mousePos}");
        }
    }

    private void DropCircle(Vector2 dropPosition)
    {
        Vector2 gridPos = grid.GlobalPosition;
        Vector2 gridSize = grid.Size;
        Vector2 cellSize = new Vector2(gridSize.X / 3, grid.Size.Y / 3);

        if (gameEnded)
        {
            GD.Print("DropCircle blocked: game has ended");
            return;
        }

        if (new Rect2(gridPos, gridSize).HasPoint(dropPosition))
        {
            Vector2 circleCenter = dropPosition - dragOffset + (draggedCircle.Size / 2);
            int row = (int)((circleCenter.Y - gridPos.Y) / cellSize.Y);
            int col = (int)((circleCenter.X - gridPos.X) / cellSize.X);

            if (row >= 0 && row < 3 && col >= 0 && col < 3)
            {
                string existingCircleName = board[row, col];
                int draggedSize = GetCircleSize(draggedCircle.Name);

                if (existingCircleName == "" || CanOverride(existingCircleName, draggedSize))
                {
                    if (existingCircleName != "")
                    {
                        TextureRect existingCircle = FindCircleByName(existingCircleName);
                        if (existingCircle != null)
                        {
                            existingCircle.GetParent().RemoveChild(existingCircle);
                            existingCircle.QueueFree();
                            GD.Print($"Removed {existingCircleName} from ({row}, {col})");
                        }
                    }

                    board[row, col] = draggedCircle.Name;
                    draggedCircle.Position = gridPos + new Vector2(col * cellSize.X, row * cellSize.Y) +
                        (cellSize - draggedCircle.Size) / 2;
                    GD.Print($"{currentPlayer} placed {draggedCircle.Name} at ({row}, {col})");

                    string newPlayer = currentPlayer == "Player1" ? "Player2" : "Player1";
                    // Добавляем newPlayer в сообщение
                    SendMessage($"move:{draggedCircle.Name}:{row}:{col}:{draggedCircle.Modulate.ToHtml()}:{draggedCircle.Size.X}:{draggedCircle.Size.Y}:{newPlayer}");
                    currentPlayer = newPlayer;
                    ui.UpdateStatus($"Ход {(currentPlayer == "Player1" ? player1Label.Text : player2Label.Text)}");
                    CheckForWinOrDraw(); // Уже есть, но проверяем
                }
                else
                {
                    ResetCirclePosition(draggedCircle);
                    GD.Print($"Cannot place {draggedCircle.Name} over {existingCircleName} at ({row}, {col})");
                }
            }
            else
            {
                ResetCirclePosition(draggedCircle);
            }
        }
        else
        {
            ResetCirclePosition(draggedCircle);
        }
    }


    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void SyncRemoveFromTable(string circleName, bool isServer)
    {
        GD.Print($"SyncRemoveFromTable called by {Multiplayer.GetUniqueId()}: {circleName}, IsServer: {isServer}");
        Control table = isServer ? player1Table : player2Table;
        TextureRect circle = table.GetNodeOrNull<TextureRect>(circleName);
        if (circle != null)
        {
            circle.GetParent().RemoveChild(circle);
            circle.QueueFree();
            GD.Print($"Removed {circleName} from {(isServer ? "Player1Table" : "Player2Table")}");
        }
    }

    /*[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void SyncCurrentPlayer(string newPlayer)
    {
        if (!isInitialized) return; // Игнорируем, если сцена ещё не готова
        currentPlayer = newPlayer;
        var global = GetNode<Global>("/root/Global");
        string currentNickname = currentPlayer == "Player1" ? 
            (Multiplayer.IsServer() ? global.PlayerNickname : global.OpponentNickname) : 
            (Multiplayer.IsServer() ? global.OpponentNickname : global.PlayerNickname);
        GD.Print($"Current player synced to {currentPlayer} by {Multiplayer.GetUniqueId()}");
        ui.UpdateStatus($"Ход {currentNickname}");
    }*/

    private void SyncCurrentPlayer(string newPlayer)
    {
        currentPlayer = newPlayer;
        var global = GetNode<Global>("/root/Global");
        string currentNickname = currentPlayer == "Player1" ? 
            (multiplayerManager.IsHost() ? global.PlayerNickname : global.OpponentNickname) : 
            (multiplayerManager.IsHost() ? global.OpponentNickname : global.PlayerNickname);
        GD.Print($"Current player set to {currentPlayer} ({currentNickname})");
        ui.UpdateStatus($"Ход {currentNickname}");
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void SyncResetGame()
    {
        GD.Print($"SyncResetGame called by {Multiplayer.GetUniqueId()}");
        ResetGame();
    }

    public void ResetGame()
    {
        GD.Print("Resetting game...");
        gameEnded = false;
        draggedCircle = null;
        currentPlayer = "Player1";

        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                board[i, j] = "";

        ClearCirclesFrom(player1Table.GetChildren());
        ClearCirclesFrom(player2Table.GetChildren());
        ClearCirclesFrom(GetChildren());

        CreateCircles(player1Table, "P1");
        CreateCircles(player2Table, "P2");

        if (Multiplayer.IsServer())
        {
            RpcId(0, nameof(SyncCurrentPlayer), currentPlayer);
        }

        ui.UpdateStatus($"Ход игрока {(currentPlayer == "Player1" ? "1" : "2")}");
    }

    private void CreateCircles(Control table, string playerPrefix)
    {
        Texture2D texture = GD.Load<Texture2D>("res://Sprites/icon.svg") ?? GD.Load<Texture2D>("res://icon.png");
        Vector2[] sizes = { new Vector2(50, 50), new Vector2(75, 75), new Vector2(100, 100) };
        string[] sizeNames = { "Small", "Medium", "Large" };
        float tableWidth = table.Size.X;
        float totalWidth = sizes[0].X * 2 + sizes[1].X * 2 + sizes[2].X * 2;
        float spacing = (tableWidth - totalWidth) / 7f;
        float currentX = spacing;

        GD.Print($"{table.Name} width: {tableWidth}, totalWidth: {totalWidth}, spacing: {spacing}");

        for (int sizeIdx = 0; sizeIdx < 3; sizeIdx++)
        {
            for (int i = 0; i < 2; i++)
            {
                TextureRect circle = new TextureRect
                {
                    Name = $"{playerPrefix}_{sizeNames[sizeIdx]}Circle{i + 1}",
                    Texture = texture,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    Size = sizes[sizeIdx],
                    Position = new Vector2(currentX, (table.Size.Y - sizes[sizeIdx].Y) / 2),
                    MouseFilter = Control.MouseFilterEnum.Stop,
                    Modulate = playerPrefix == "P1" ? new Color(1, 0, 0) : new Color(0, 0, 1)
                };
                table.AddChild(circle);
                initialPositions[circle.Name] = circle.Position;
                initialParents[circle.Name] = table;
                GD.Print($"Created {circle.Name} at {circle.Position}, size: {circle.Size}");
                currentX += sizes[sizeIdx].X + spacing;
            }
        }
    }

    private void UpdateHighlight(Vector2 mousePosition)
    {
        Vector2 gridPos = grid.GlobalPosition;
        Vector2 gridSize = grid.Size;
        Vector2 cellSize = new Vector2(gridSize.X / 3, grid.Size.Y / 3);

        if (new Rect2(gridPos, gridSize).HasPoint(mousePosition))
        {
            Vector2 circleCenter = mousePosition - dragOffset + (draggedCircle.Size / 2);
            int row = (int)((circleCenter.Y - gridPos.Y) / cellSize.Y);
            int col = (int)((circleCenter.X - gridPos.X) / cellSize.X);

            if (row >= 0 && row < 3 && col >= 0 && col < 3)
            {
                int buttonIndex = row * 3 + col;
                Button button = gridButtons[buttonIndex];
                string existingCircleName = board[row, col];
                int draggedSize = GetCircleSize(draggedCircle.Name);

                if (existingCircleName == "" || CanOverride(existingCircleName, draggedSize))
                {
                    if (highlightedButton != button)
                    {
                        ClearHighlight();
                        highlightedButton = button;
                        highlightedButton.Modulate = new Color(0.5f, 1.0f, 0.5f, 1.0f);
                        GD.Print($"Highlighted Button{buttonIndex} at ({row}, {col})");
                    }
                    return;
                }
            }
        }
        ClearHighlight();
    }

    private void ClearHighlight()
    {
        if (highlightedButton != null)
        {
            highlightedButton.Modulate = new Color(1, 1, 1, 1);
            highlightedButton = null;
        }
    }

    private void CheckForWinOrDraw()
    {
        GD.Print("Checking for win or draw...");
        for (int i = 0; i < WinningCombinations.GetLength(0); i++)
        {
            int a = WinningCombinations[i, 0];
            int b = WinningCombinations[i, 1];
            int c = WinningCombinations[i, 2];

            string cellA = board[a / 3, a % 3];
            string cellB = board[b / 3, b % 3];
            string cellC = board[c / 3, c % 3];

            GD.Print($"Combo {i}: ({a/3},{a%3})={cellA}, ({b/3},{b%3})={cellB}, ({c/3},{c%3})={cellC}");

            // Проверяем, что все ячейки заняты и принадлежат одному игроку
            if (cellA != "" && cellB != "" && cellC != "" &&
                cellA.StartsWith("P1") == cellB.StartsWith("P1") && 
                cellB.StartsWith("P1") == cellC.StartsWith("P1"))
            {
                string winner = cellA.StartsWith("P1") ? "Player1" : "Player2";
                GD.Print($"{winner} wins!");
                string winnerName = winner == "Player1" ? player1Label.Text : player2Label.Text;
                ui.UpdateStatus($"{winnerName} победил!");
                gameEnded = true;

                SendMessage($"game_over:win:{winner}");
                GD.Print($"Game ended with winner: {winner}");
                return;
            }
        }

        bool isDraw = true;
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                if (board[i, j] == "")
                {
                    isDraw = false;
                    break;
                }

        if (isDraw)
        {
            GD.Print("Draw!");
            ui.UpdateStatus("Ничья!");
            gameEnded = true;

            SendMessage("game_over:draw");
            GD.Print("Game ended with draw");
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void SyncGameEnd(string message)
    {
        gameEnded = true;
        ui.UpdateStatus(message);
        backToMenuButton.Disabled = false; // Активируем кнопку "Back to Menu"
    }
    private string CheckForWin()
    {
        for (int i = 0; i < WinningCombinations.GetLength(0); i++)
        {
            int a = WinningCombinations[i, 0];
            int b = WinningCombinations[i, 1];
            int c = WinningCombinations[i, 2];
            string cellA = board[a / 3, a % 3];
            string cellB = board[b / 3, b % 3];
            string cellC = board[c / 3, c % 3];

            if (!string.IsNullOrEmpty(cellA) && cellA.StartsWith("P1") && cellB.StartsWith("P1") && cellC.StartsWith("P1"))
                return "Player1";
            if (!string.IsNullOrEmpty(cellA) && cellA.StartsWith("P2") && cellB.StartsWith("P2") && cellC.StartsWith("P2"))
                return "Player2";
        }
        return null;
    }

    private bool IsBoardFull()
    {
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                if (string.IsNullOrEmpty(board[i, j]))
                    return false;
        return true;
    }

    private int GetCircleSize(string circleName)
    {
        if (string.IsNullOrEmpty(circleName)) return 0;
        if (circleName.Contains("Small")) return 1;
        if (circleName.Contains("Medium")) return 2;
        if (circleName.Contains("Large")) return 3;
        return 0;
    }

    private bool CanOverride(string existingCircleName, int newCircleSize)
    {
        return newCircleSize > GetCircleSize(existingCircleName);
    }

    private TextureRect FindCircleByName(string name)
    {
        foreach (Node child in GetChildren())
        {
            TextureRect circle = child as TextureRect;
            if (circle != null && circle.Name == name)
                return circle;
        }
        return null;
    }

    private void ResetCirclePosition(TextureRect circle)
    {
        if (initialPositions.TryGetValue(circle.Name, out Vector2 pos) && initialParents.TryGetValue(circle.Name, out Node parent))
        {
            circle.GetParent().RemoveChild(circle);
            parent.AddChild(circle);
            circle.Position = pos;
            GD.Print($"Reset {circle.Name} to initial position {circle.Position} in {parent.Name}");
        }
        else
        {
            GD.Print($"Ошибка: Исходная позиция или родитель для {circle.Name} не найдены!");
        }
    }

    private void ClearCirclesFrom(Godot.Collections.Array<Godot.Node> children)
    {
        foreach (Node child in children)
        {
            TextureRect circle = child as TextureRect;
            if (circle != null)
            {
                circle.GetParent().RemoveChild(circle);
                circle.QueueFree();
            }
        }
    }


    private int CalculateFontSize()
    {
        Vector2 screenSize = GetViewport().GetVisibleRect().Size;
        return Mathf.Clamp((int)(screenSize.X / 10f), 24, 64);
    }
}