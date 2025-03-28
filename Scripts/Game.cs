using Godot;
using System.Collections.Generic;

public partial class Game : Control
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
    private readonly Dictionary<string, Vector2> initialPositions = new();
    private readonly Dictionary<string, Node> initialParents = new();
    private Button highlightedButton = null;
    private Button[] gridButtons;
    private Button backToMenuButton;
    private Button restartButton;
    private bool restartRequested = false;
    private MultiplayerManager multiplayerManager;
    private GameOverModal gameOverModal;
    private TextureRect[] tiles;
    public string gameMode = "multiplayer";

    private static readonly int[,] WinningCombinations = new int[,]
    {
        {0, 1, 2}, {3, 4, 5}, {6, 7, 8},
        {0, 3, 6}, {1, 4, 7}, {2, 5, 8},
        {0, 4, 8}, {2, 4, 6}
    };

    public override void _Ready()
    {
        grid = GetNode<GridContainer>("Grid");
        player1Table = GetNode<Control>("Player1TableContainer/Player1Table");
        player2Table = GetNode<Control>("Player2TableContainer/Player2Table");
        player1Label = GetNode<Label>("Player1TableContainer/Player1Label");
        player2Label = GetNode<Label>("Player2TableContainer/Player2Label");
        ui = GetNode<UI>("UI");
        backToMenuButton = GetNode<Button>("UI/BackToMenuButton");
        restartButton = GetNode<Button>("UI/RestartButton");
        multiplayerManager = GetNode<MultiplayerManager>("/root/MultiplayerManager");
        gameOverModal = GetNode<GameOverModal>("GameOverModal");

        if (!ValidateNodes()) return;

        gameOverModal.Visible = false;
        restartRequested = false;

        CreateGameField();
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                board[i, j] = "";

        SetupDeviceLayout();

        float buttonSize = grid.Size.X / 3;
        for (int i = 0; i < 9; i++)
        {
            gridButtons[i].CustomMinimumSize = new Vector2(buttonSize, buttonSize);
            gridButtons[i].Size = new Vector2(buttonSize, buttonSize);
            if (tiles[i] != null)
            {
                tiles[i].Position = Vector2.Zero;
                tiles[i].Size = gridButtons[i].Size;
            }
        }

        CreateCircles(player1Table, "P1");
        CreateCircles(player2Table, "P2");

        var global = GetNode<Global>("/root/Global");
        if (multiplayerManager.IsHost())
        {
            currentPlayer = "Player1";
            player1Label.Text = global.PlayerNickname;
            player2Label.Text = global.OpponentNickname;
            ui.UpdateStatus($"{global.PlayerNickname}'s turn");
            SendMessage($"sync_labels:{global.PlayerNickname}");
            SendMessage($"sync_current_player:{currentPlayer}");
        }
        else
        {
            currentPlayer = "Player1";
            player1Label.Text = global.PlayerNickname;
            player2Label.Text = global.OpponentNickname;
            ui.UpdateStatus($"{global.OpponentNickname}'s turn");
            SendMessage($"sync_labels:{global.PlayerNickname}");
        }

        multiplayerManager.Connect("MessageReceived", Callable.From((string message) => ProcessMessage(message)));
        // MODIFIED: Replace the direct connection to ui.OnMenuButtonPressed with a custom handler
        backToMenuButton.Pressed += OnBackToMenuButtonPressed;
        restartButton.Pressed += ui.OnRestartButtonPressed;
        gameOverModal.MenuButton.Pressed += OnMenuButtonPressed;
        gameOverModal.RestartButton.Pressed += OnRestartButtonPressed;
    }

    // NEW: Custom handler for backToMenuButton.Pressed
    private void OnBackToMenuButtonPressed()
    {
        var global = GetNode<Global>("/root/Global");
        // Send a message to the other player before leaving
        SendMessage($"player_left:{global.PlayerNickname}");
        // Transition to the menu scene for the player who pressed the button
        GetTree().ChangeSceneToFile("res://Scenes/Menu.tscn");
    }

    private bool ValidateNodes()
    {
        if (grid == null || player1Table == null || player2Table == null || 
            player1Label == null || player2Label == null || ui == null || 
            backToMenuButton == null || restartButton == null || multiplayerManager == null || gameOverModal == null)
        {
            //GD.PrintErr("Ошибка: Один из узлов не найден!");
            return false;
        }

        if (gameOverModal.ResultLabel == null || gameOverModal.InstructionLabel == null || 
            gameOverModal.MenuButton == null || gameOverModal.RestartButton == null)
        {
            //GD.PrintErr("Ошибка: Один из элементов GameOverModal не привязан!");
            return false;
        }
        return true;
    }

    private void SetupDeviceLayout()
{
    // Получаем размер окна из DisplayManager
    var displayManager = GetNode<DisplayManager>("/root/DisplayManager");
    Vector2 screenSize = displayManager.GetWindowSize();

    // Масштабируем размер сетки до 40% ширины экрана (768 пикселей для 1920 пикселей)
    float gridSize = screenSize.X * 0.4f; // 768 пикселей для 1920 пикселей
    grid.Position = new Vector2((screenSize.X - gridSize) / 2, screenSize.Y * 0.15f + 50); // Центрируем по горизонтали, 15% от верха
    grid.Size = new Vector2(gridSize, gridSize);
    grid.Visible = true;
    //GD.Print($"Позиция сетки: {grid.Position}, Размер: {grid.Size}");

    var player1TableContainer = GetNode<Control>("Player1TableContainer");
    float player1TableWidth = screenSize.X * 0.25f;
    float player1TableHeight = gridSize;
    player1TableContainer.Position = new Vector2(grid.Position.X + gridSize + screenSize.X * 0.02f, grid.Position.Y + 50);
    player1TableContainer.Size = new Vector2(player1TableWidth, player1TableHeight);
    player1TableContainer.Visible = true;

    player1Table.Position = new Vector2(0, 0);
    player1Table.Size = new Vector2(player1TableWidth, player1TableHeight - player1Label.Size.Y - 10);
    player1Table.Visible = true;

    player1Label.Position = new Vector2((player1TableWidth - player1Label.Size.X) / 2, player1Table.Size.Y);
    //GD.Print($"Позиция Player1TableContainer: {player1TableContainer.Position}, Размер: {player1TableContainer.Size}");
    //GD.Print($"Позиция Player1Table: {player1Table.Position}, Размер: {player1Table.Size}");
    //GD.Print($"Позиция Player1Label: {player1Label.Position}, Размер: {player1Label.Size}");

    var player2TableContainer = GetNode<Control>("Player2TableContainer");
    float player2TableWidth = screenSize.X * 0.25f;
    float player2TableHeight = gridSize;
    player2TableContainer.Position = new Vector2(screenSize.X * 0.02f, grid.Position.Y + 50);
    player2TableContainer.Size = new Vector2(player2TableWidth, player2TableHeight);
    player2TableContainer.Visible = true;

    player2Table.Position = new Vector2(0, 0);
    player2Table.Size = new Vector2(player2TableWidth, player2TableHeight - player2Label.Size.Y - 10);
    player2Table.Visible = true;

    player2Label.Position = new Vector2((player2TableWidth - player2Label.Size.X) / 2, player2Table.Size.Y );
    //GD.Print($"Позиция Player2TableContainer: {player2TableContainer.Position}, Размер: {player2TableContainer.Size}");
    //GD.Print($"Позиция Player2Table: {player2Table.Position}, Размер: {player2Table.Size}");
    //GD.Print($"Позиция Player2Label: {player2Label.Position}, Размер: {player2Label.Size}");

    // Настраиваем размер и позицию GameOverModal
    //gameOverModal.Position = new Vector2((screenSize.X - gameOverModal.Size.X) / 2, (screenSize.Y - gameOverModal.Size.Y) / 2);
    //gameOverModal.Size = new Vector2(screenSize.X * 0.3f, screenSize.Y * 0.3f); // 30% ширины и высоты экрана
    //GD.Print($"Позиция GameOverModal: {gameOverModal.Position}, Размер: {gameOverModal.Size}");

    if (!multiplayerManager.IsHost())
    {
        (player1TableContainer.Position, player2TableContainer.Position) = (player2TableContainer.Position, player1TableContainer.Position);
        UpdateCirclePositions(player1Table);
        UpdateCirclePositions(player2Table);

        player1Label.Position = new Vector2((player1TableWidth - player1Label.Size.X) / 2, player1Table.Size.Y + 10);
        player2Label.Position = new Vector2((player2TableWidth - player2Label.Size.X) / 2, player2Table.Size.Y + 10);
    }
}

    private void CreateGameField()
    {
        grid.Columns = 3;
        gridButtons = new Button[9];
        tiles = new TextureRect[9];
        grid.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0) });
        grid.Modulate = new Color(1, 1, 1, 1);

        Texture2D tileTexture = GD.Load<Texture2D>("res://Sprites/grass_tile_no_bg.png");
        //if (tileTexture == null) GD.PrintErr("Не удалось загрузить текстуру: res://Sprites/grass_tile_no_bg.png");

        for (int i = 0; i < 9; i++)
        {
            Button button = new()
            {
                Name = "Button" + i,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                Disabled = true,
                Visible = true,
                Flat = true
            };
            button.AddThemeFontSizeOverride("font_size", CalculateFontSize());
            button.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
            button.Modulate = new Color(1, 1, 1, 1);

            if (tileTexture != null)
            {
                TextureRect tile = new()
                {
                    Name = "Tile" + i,
                    Texture = tileTexture,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspect,
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                    Visible = true
                };
                button.AddChild(tile);
                tiles[i] = tile;
            }

            grid.AddChild(button);
            gridButtons[i] = button;
        }
    }

    private void CreateCircles(Control table, string playerPrefix)
    {
        string texturePath = playerPrefix == "P1" ? "res://Sprites/red_cup.png" : "res://Sprites/blue_cup.png";
        Texture2D texture = GD.Load<Texture2D>(texturePath);
        if (texture == null)
        {
            //GD.PrintErr($"Не удалось загрузить текстуру: {texturePath}");
            return;
        }

        float tableWidth = table.Size.X;
        float tableHeight = table.Size.Y;
        bool isMobile = OS.GetName() == "Android" || OS.GetName() == "iOS";
        float baseSize = isMobile ? tableHeight * 0.8f : Mathf.Min((tableWidth / 2) * 0.9f, (tableHeight / 3) * 0.9f);

        Vector2[] sizes = { new(baseSize * 0.7f, baseSize * 0.7f), new(baseSize * 0.85f, baseSize * 0.85f), new(baseSize, baseSize) };
        string[] sizeNames = { "Small", "Medium", "Large" };

        float spacingX = Mathf.Min((tableWidth - (sizes[0].X * 2)) / 3, 10f);
        float spacingY = Mathf.Min((tableHeight - (sizes[0].Y + sizes[1].Y + sizes[2].Y)) / 4, 10f);
        if (spacingX < 2f) spacingX = 2f;
        if (spacingY < 2f) spacingY = 2f;

        float currentY = spacingY;
        for (int row = 0; row < 3; row++)
        {
            int sizeIdx = row;
            float columnWidth = tableWidth / 2;
            float leftCircleX = (columnWidth - sizes[sizeIdx].X) / 2;
            float rightCircleX = columnWidth + (columnWidth - sizes[sizeIdx].X) / 2;

            TextureRect circle1 = new()
            {
                Name = $"{playerPrefix}_{sizeNames[sizeIdx]}Circle1",
                Texture = texture,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                Size = sizes[sizeIdx],
                Position = new Vector2(leftCircleX, currentY),
                MouseFilter = Control.MouseFilterEnum.Stop,
                Visible = true
            };
            table.AddChild(circle1);
            initialPositions[circle1.Name] = circle1.Position;
            initialParents[circle1.Name] = table;

            TextureRect circle2 = new()
            {
                Name = $"{playerPrefix}_{sizeNames[sizeIdx]}Circle2",
                Texture = texture,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                Size = sizes[sizeIdx],
                Position = new Vector2(rightCircleX, currentY),
                MouseFilter = Control.MouseFilterEnum.Stop,
                Visible = true
            };
            table.AddChild(circle2);
            initialPositions[circle2.Name] = circle2.Position;
            initialParents[circle2.Name] = table;

            currentY += sizes[sizeIdx].Y + spacingY;
        }
    }

    private void UpdateCirclePositions(Control table)
    {
        float tableWidth = table.Size.X;
        float tableHeight = table.Size.Y;
        float baseSize = Mathf.Min((tableWidth / 2) * 0.9f, (tableHeight / 3) * 0.9f);
        Vector2[] sizes = { new(baseSize * 0.7f, baseSize * 0.7f), new(baseSize * 0.85f, baseSize * 0.85f), new(baseSize, baseSize) };

        float spacingX = Mathf.Min((tableWidth - (sizes[0].X * 2)) / 3, 10f);
        float spacingY = Mathf.Min((tableHeight - (sizes[0].Y + sizes[1].Y + sizes[2].Y)) / 4, 10f);
        if (spacingX < 2f) spacingX = 2f;
        if (spacingY < 2f) spacingY = 2f;

        float currentY = spacingY;
        foreach (Node child in table.GetChildren())
        {
            if (child is TextureRect circle)
            {
                int sizeIdx = circle.Name.ToString().Contains("Small") ? 0 : circle.Name.ToString().Contains("Medium") ? 1 : 2;
                circle.Size = sizes[sizeIdx];
                float columnWidth = tableWidth / 2;
                float leftCircleX = (columnWidth - sizes[sizeIdx].X) / 2;
                float rightCircleX = columnWidth + (columnWidth - sizes[sizeIdx].X) / 2;

                circle.Position = circle.Name.ToString().EndsWith("Circle1") ? new Vector2(leftCircleX, currentY) : new Vector2(rightCircleX, currentY);
                if (circle.Name.ToString().EndsWith("Circle2")) currentY += sizes[sizeIdx].Y + spacingY;
            }
        }
    }

    private void ProcessMessage(string message)
    {
        var global = GetNode<Global>("/root/Global");
        string[] parts = message.Split(':');

        switch (parts[0])
        {
            case "sync_labels":
                if (parts.Length != 2) return;
                string senderNickname = parts[1];
                if (multiplayerManager.IsHost())
                {
                    global.OpponentNickname = senderNickname;
                    player2Label.Text = senderNickname;
                    SendMessage($"update_labels:{global.PlayerNickname}:{senderNickname}");
                }
                else
                {
                    global.OpponentNickname = senderNickname;
                    player2Label.Text = senderNickname;
                    SendMessage($"update_labels:{senderNickname}:{global.PlayerNickname}");
                }
                break;

            case "update_labels":
                if (parts.Length != 3) return;
                string serverNickname = parts[1];
                string clientNickname = parts[2];
                if (multiplayerManager.IsHost())
                {
                    player1Label.Text = global.PlayerNickname;
                    player2Label.Text = clientNickname;
                    global.OpponentNickname = clientNickname;
                }
                else
                {
                    player1Label.Text = global.PlayerNickname;
                    player2Label.Text = serverNickname;
                    global.OpponentNickname = serverNickname;
                }
                break;

            case "sync_current_player":
                if (parts.Length != 2) return;
                string newPlayer = parts[1];
                SyncCurrentPlayer(newPlayer);
                break;

            case "move":
                if (parts.Length != 8) return;
                string circleName = parts[1];
                int row = int.Parse(parts[2]);
                int col = int.Parse(parts[3]);
                Color color = new(parts[4]);
                Vector2 size = new(float.Parse(parts[5]), float.Parse(parts[6]));
                string newPlayerMove = parts[7];
                SyncMove(circleName, row, col, color, size);
                SyncCurrentPlayer(newPlayerMove);
                break;

            case "game_over":
                if (parts.Length < 2) return;
                string result = parts[1];
                if (result == "win" && parts.Length == 3)
                {
                    string winner = parts[2];
                    string winnerName = winner == "Player1" ? player1Label.Text : player2Label.Text;
                    gameOverModal.ResultLabel.Text = $"{winnerName} wins!";
                    gameOverModal.Visible = true;
                    gameEnded = true;
                }
                else if (result == "draw")
                {
                    gameOverModal.ResultLabel.Text = "Draw!";
                    gameOverModal.Visible = true;
                    gameEnded = true;
                }
                break;

            case "restart_request":
                if (parts.Length != 2) return;
                restartRequested = true;
                gameOverModal.InstructionLabel.Text = $"Player {parts[1]} is waiting";
                //ui.UpdateStatus($"Игрок {parts[1]} ожидает");
                gameOverModal.Visible = true;
                break;

            case "restart_confirmed":
                GetTree().ReloadCurrentScene();
                break;

            case "player_left":
                if (parts.Length != 2) return;
                // MODIFIED: Update GameOverModal for the remaining player
                gameOverModal.ResultLabel.Text = "Game Over";
                gameOverModal.InstructionLabel.Text = $"Player {parts[1]} has left the game";
                gameOverModal.RestartButton.Disabled = true; // Disable the Restart button
                gameOverModal.Visible = true;
                gameEnded = true;
                break;
        }
    }

    private void SyncMove(string circleName, int row, int col, Color color, Vector2 size)
    {
        if (gameEnded) return;

        TextureRect circle = FindCircleByName(circleName) ?? CreateOrMoveCircle(circleName, color, size);
        string existingCircleName = board[row, col];
        if (existingCircleName != "" && existingCircleName != circleName)
        {
            TextureRect existingCircle = FindCircleByName(existingCircleName);
            if (existingCircle != null)
            {
                existingCircle.GetParent().RemoveChild(existingCircle);
                existingCircle.QueueFree();
            }
        }

        Vector2 gridPos = grid.GlobalPosition;
        Vector2 cellSize = new(grid.Size.X / 3, grid.Size.Y / 3);
        if (circle.GetParent() != this)
        {
            circle.GetParent().RemoveChild(circle);
            AddChild(circle);
        }
        circle.Position = gridPos + new Vector2(col * cellSize.X, row * cellSize.Y) + (cellSize - circle.Size) / 2;
        board[row, col] = circleName;
        CheckForWinOrDraw();
    }

    private TextureRect CreateOrMoveCircle(string circleName, Color color, Vector2 size)
    {
        Control table = circleName.StartsWith("P1") ? player1Table : player2Table;
        TextureRect circle = table.GetNodeOrNull<TextureRect>(circleName);
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
        }
        else
        {
            circle.GetParent().RemoveChild(circle);
            AddChild(circle);
        }
        return circle;
    }

    private void SendMessage(string message)
    {
        var wsPeer = multiplayerManager.GetWebSocketPeer();
        if (wsPeer != null && wsPeer.GetReadyState() == WebSocketPeer.State.Open)
            wsPeer.SendText(message);
        else
            GD.PrintErr($"Failed to send message: {message}, WebSocket state: {wsPeer?.GetReadyState()}");
    }

    public override void _Input(InputEvent @event)
    {
        if (gameEnded) return;

        bool isMyTurn = (multiplayerManager.IsHost() && currentPlayer == "Player1") || 
                        (!multiplayerManager.IsHost() && currentPlayer == "Player2");
        if (!isMyTurn) return;

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
        Control table = multiplayerManager.IsHost() ? player1Table : player2Table;
        string prefix = multiplayerManager.IsHost() ? "P1" : "P2";

        foreach (Node child in table.GetChildren())
        {
            if (child is TextureRect circle && circle.Name.ToString().StartsWith(prefix))
            {
                Rect2 circleRect = new(circle.GlobalPosition, circle.Size);
                if (circleRect.HasPoint(mousePos))
                {
                    draggedCircle = circle;
                    dragOffset = mousePos - circle.GlobalPosition;
                    circle.GetParent().RemoveChild(circle);
                    AddChild(circle);
                    circle.Position = mousePos - dragOffset;
                    break;
                }
            }
        }
    }

    private void DropCircle(Vector2 dropPosition)
    {
        if (gameEnded) return;

        Vector2 gridPos = grid.GlobalPosition;
        Vector2 gridSize = grid.Size;
        Vector2 cellSize = new(gridSize.X / 3, gridSize.Y / 3);

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
                        }
                    }

                    board[row, col] = draggedCircle.Name;
                    draggedCircle.Position = gridPos + new Vector2(col * cellSize.X, row * cellSize.Y) + (cellSize - draggedCircle.Size) / 2;

                    string newPlayer = currentPlayer == "Player1" ? "Player2" : "Player1";
                    SendMessage($"move:{draggedCircle.Name}:{row}:{col}:{draggedCircle.Modulate.ToHtml()}:{draggedCircle.Size.X}:{draggedCircle.Size.Y}:{newPlayer}");
                    currentPlayer = newPlayer;
                    ui.UpdateStatus($"{(currentPlayer == "Player1" ? player1Label.Text : player2Label.Text)}'s turn");
                    CheckForWinOrDraw();
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
        else
        {
            ResetCirclePosition(draggedCircle);
        }
    }

    private void SyncCurrentPlayer(string newPlayer)
    {
        currentPlayer = newPlayer;
        var global = GetNode<Global>("/root/Global");
        string currentNickname = currentPlayer == "Player1" ? 
            (multiplayerManager.IsHost() ? global.PlayerNickname : global.OpponentNickname) : 
            (multiplayerManager.IsHost() ? global.OpponentNickname : global.PlayerNickname);
        ui.UpdateStatus($"{currentNickname}'s turn");
    }

    private void UpdateHighlight(Vector2 mousePosition)
    {
        Vector2 gridPos = grid.GlobalPosition;
        Vector2 gridSize = grid.Size;
        Vector2 cellSize = new(gridSize.X / 3, gridSize.Y / 3);

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
        for (int i = 0; i < WinningCombinations.GetLength(0); i++)
        {
            int a = WinningCombinations[i, 0];
            int b = WinningCombinations[i, 1];
            int c = WinningCombinations[i, 2];

            string cellA = board[a / 3, a % 3];
            string cellB = board[b / 3, b % 3];
            string cellC = board[c / 3, c % 3];

            if (cellA != "" && cellB != "" && cellC != "" &&
                cellA.StartsWith("P1") == cellB.StartsWith("P1") && 
                cellB.StartsWith("P1") == cellC.StartsWith("P1"))
            {
                string winner = cellA.StartsWith("P1") ? "Player1" : "Player2";
                string winnerName = winner == "Player1" ? player1Label.Text : player2Label.Text;
                gameOverModal.ResultLabel.Text = $"{winnerName} победил!";
                gameOverModal.Visible = true;
                ui.GetNode<Button>("RestartButton").Disabled = true;
                ui.GetNode<Button>("BackToMenuButton").Disabled = true;
                gameEnded = true;
                SendMessage($"game_over:win:{winner}");
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
            gameOverModal.ResultLabel.Text = "Ничья!";
            gameOverModal.Visible = true;
            gameEnded = true;
            ui.GetNode<Button>("RestartButton").Disabled = true;
            ui.GetNode<Button>("BackToMenuButton").Disabled = true;
            SendMessage("game_over:draw");
        }
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
            if (child is TextureRect circle && circle.Name == name)
                return circle;
        return null;
    }

    private void ResetCirclePosition(TextureRect circle)
    {
        if (initialPositions.TryGetValue(circle.Name, out Vector2 pos) && initialParents.TryGetValue(circle.Name, out Node parent))
        {
            circle.GetParent().RemoveChild(circle);
            parent.AddChild(circle);
            circle.Position = pos;
        }
    }

    public void OnRestartButtonPressed()
    {
        var global = GetNode<Global>("/root/Global");
        if (!restartRequested)
        {
            SendMessage($"restart_request:{global.PlayerNickname}");
            restartRequested = true;
            gameOverModal.InstructionLabel.Text = $"Waiting for the second player...";
            //ui.UpdateStatus($"Ожидание второго игрока...");
        }
        else
        {
            SendMessage("restart_confirmed");
            GetTree().ReloadCurrentScene();
        }
    }

    private void OnMenuButtonPressed()
    {
        var global = GetNode<Global>("/root/Global");
        SendMessage($"player_left:{global.PlayerNickname}");
        GetTree().ChangeSceneToFile("res://Scenes/Menu.tscn");
    }

    private int CalculateFontSize()
    {
        Vector2 screenSize = GetViewport().GetVisibleRect().Size;
        return Mathf.Clamp((int)(screenSize.X / 10f), 24, 64);
    }

    public void ResetGame()
    {
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

        SetupDeviceLayout();

        var global = GetNode<Global>("/root/Global");
        if (multiplayerManager.IsHost())
        {
            player1Label.Text = global.PlayerNickname;
            player2Label.Text = global.OpponentNickname;
            SendMessage($"sync_current_player:{currentPlayer}");
        }
        else
        {
            player1Label.Text = global.PlayerNickname;
            player2Label.Text = global.OpponentNickname;
        }

        string currentNickname = currentPlayer == "Player1" ? 
            (multiplayerManager.IsHost() ? global.PlayerNickname : global.OpponentNickname) : 
            (multiplayerManager.IsHost() ? global.OpponentNickname : global.PlayerNickname);
        ui.UpdateStatus($"{currentNickname} turn");
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void SyncResetGame()
    {
        ResetGame();
    }

    private void ClearCirclesFrom(Godot.Collections.Array<Godot.Node> children)
    {
        foreach (Node child in children)
        {
            if (child is TextureRect circle)
            {
                circle.GetParent().RemoveChild(circle);
                circle.QueueFree();
            }
        }
    }
}