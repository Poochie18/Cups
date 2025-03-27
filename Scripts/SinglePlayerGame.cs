using Godot;
using System.Collections.Generic;

public partial class SinglePlayerGame : Control
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
    private string gameMode = "friend";
    private bool isBotThinking = false;
    private BotAI botAI;
    private int botDifficulty = 0;
    private StyleBoxFlat normalStyle;
    private StyleBoxFlat highlightStyle;

    private static readonly int[,] WinningCombinations = new int[,]
    {
        {0, 1, 2}, {3, 4, 5}, {6, 7, 8},
        {0, 3, 6}, {1, 4, 7}, {2, 5, 8},
        {0, 4, 8}, {2, 4, 6}
    };

    public void SetGameMode(string mode, int difficulty = 0)
    {
        gameMode = mode;
        botDifficulty = difficulty;
        if (mode == "bot" && player2Table != null && player1Table != null)
        {
            botAI = new BotAI(board, player2Table, player1Table, difficulty);
        }
        GD.Print($"Режим игры установлен: {gameMode}, Сложность бота: {difficulty}");
    }

    public override void _Ready()
    {
        grid = GetNode<GridContainer>("Grid");
        player1Table = GetNode<Control>("Player1TableContainer/Player1Table");
        player2Table = GetNode<Control>("Player2TableContainer/Player2Table");
        player1Label = GetNode<Label>("Player1TableContainer/Player1Label");
        player2Label = GetNode<Label>("Player2TableContainer/Player2Label");
        ui = GetNode<UI>("UI");

        if (grid == null || player1Table == null || player2Table == null || 
            player1Label == null || player2Label == null || ui == null)
        {
            GD.PrintErr("Ошибка: Один из узлов не найден!");
            return;
        }

        var global = GetNode<Global>("/root/Global");
        gameMode = global.GameMode;

        if (gameMode == "bot")
        {
            SetGameMode("bot", global.BotDifficulty);
            player1Label.Text = global.PlayerNickname;
            player2Label.Text = "Bot";
            GD.Print($"Bot mode: P1Label={player1Label.Text}, P2Label={player2Label.Text}");
        }
        else
        {
            SetGameMode("friend");
            player1Label.Text = global.PlayerNickname;
            player2Label.Text = global.PlayerNickname + "_friend";
            GD.Print($"Friend mode: P1Label={player1Label.Text}, P2Label={player2Label.Text}");
        }

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
            GD.Print($"Updated Button{i} Size to: {gridButtons[i].Size}");
        }

        CreateCircles(player1Table, "P1");
        CreateCircles(player2Table, "P2");

        string initialNickname = GetCurrentPlayerNickname();
        ui.UpdateStatus($"Ход {initialNickname}");
        GD.Print($"Initial status: Ход {initialNickname}");

        ui.GetNode<Button>("RestartButton").Pressed += ui.OnRestartButtonPressed;
        ui.GetNode<Button>("BackToMenuButton").Pressed += ui.OnMenuButtonPressed;
    }

    private string GetCurrentPlayerNickname()
    {
        var global = GetNode<Global>("/root/Global");
        return currentPlayer == "Player1" ? global.PlayerNickname : 
               (gameMode == "bot" ? "Bot" : global.PlayerNickname + "_friend");
    }

    private void SetupDeviceLayout()
    {
        bool isMobile = OS.GetName() == "Android" || OS.GetName() == "iOS";
        Vector2 screenSize = GetViewport().GetVisibleRect().Size;

        if (isMobile)
        {
            DisplayServer.WindowSetSize(new Vector2I((int)screenSize.X, (int)screenSize.Y));
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Maximized);
            GD.Print("Mobile layout setup will be implemented later.");
        }
        else
        {
            DisplayServer.WindowSetSize(new Vector2I(1280, 720));
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
            screenSize = new Vector2(1280, 720);
            GD.Print($"Screen Size: {screenSize}");

            float gridSize = 500;
            grid.Position = new Vector2((screenSize.X - gridSize) / 2, 140);
            grid.Size = new Vector2(gridSize, gridSize);
            grid.Visible = true;
            GD.Print($"Grid Position: {grid.Position}, Size: {grid.Size}");

            var player1TableContainer = GetNode<Control>("Player1TableContainer");
            float player1TableWidth = (screenSize.X - gridSize) / 2 - 40;
            float player1TableHeight = gridSize;
            player1TableContainer.Position = new Vector2(grid.Position.X + gridSize + 20, 160);
            player1TableContainer.Size = new Vector2(player1TableWidth, player1TableHeight);
            player1TableContainer.Visible = true;

            player1Table.Position = new Vector2(0, 0);
            player1Table.Size = new Vector2(player1TableWidth, player1TableHeight - player1Label.Size.Y - 10);
            player1Table.Visible = true;

            player1Label.Position = new Vector2((player1TableWidth - player1Label.Size.X) / 2, player1Table.Size.Y + 10);
            GD.Print($"Player1TableContainer Position: {player1TableContainer.Position}, Size: {player1TableContainer.Size}");
            GD.Print($"Player1Table Position: {player1Table.Position}, Size: {player1Table.Size}");
            GD.Print($"Player1Label Position: {player1Label.Position}, Size: {player1Label.Size}");

            var player2TableContainer = GetNode<Control>("Player2TableContainer");
            float player2TableWidth = (screenSize.X - gridSize) / 2 - 40;
            float player2TableHeight = gridSize;
            player2TableContainer.Position = new Vector2(20, 160);
            player2TableContainer.Size = new Vector2(player2TableWidth, player2TableHeight);
            player2TableContainer.Visible = true;

            player2Table.Position = new Vector2(0, 0);
            player2Table.Size = new Vector2(player2TableWidth, player2TableHeight - player2Label.Size.Y - 10);
            player2Table.Visible = true;

            player2Label.Position = new Vector2((player2TableWidth - player2Label.Size.X) / 2, player2Table.Size.Y + 10);
            GD.Print($"Player2TableContainer Position: {player2TableContainer.Position}, Size: {player2TableContainer.Size}");
            GD.Print($"Player2Table Position: {player2Table.Position}, Size: {player2Table.Size}");
            GD.Print($"Player2Label Position: {player2Label.Position}, Size: {player2Label.Size}");
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (gameEnded || isBotThinking) return;

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
    Control table = currentPlayer == "Player1" ? player1Table : player2Table;
    foreach (Node child in table.GetChildren())
    {
        TextureRect circle = child as TextureRect; // Обновлено на TextureRect
        if (circle != null && new Rect2(circle.GlobalPosition, circle.Size).HasPoint(mousePos))
        {
            draggedCircle = circle;
            dragOffset = mousePos - circle.GlobalPosition;
            circle.GetParent().RemoveChild(circle);
            AddChild(circle);
            circle.Position = mousePos - dragOffset;
            GD.Print($"Started dragging {circle.Name} from {circle.Position} by {currentPlayer}");
            break;
        }
    }
}

    private void UpdateHighlight(Vector2 mousePosition)
    {
        Vector2 gridPos = grid.GlobalPosition;
        Vector2 gridSize = grid.Size;
        Vector2 cellSize = new Vector2(gridSize.X / 3, gridSize.Y / 3);

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
                        highlightedButton.AddThemeStyleboxOverride("normal", highlightStyle);
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
            highlightedButton.AddThemeStyleboxOverride("normal", normalStyle);
            highlightedButton = null;
        }
    }

    private void DropCircle(Vector2 dropPosition)
{
    Vector2 gridPos = grid.GlobalPosition;
    Vector2 gridSize = grid.Size;
    Vector2 cellSize = new Vector2(gridSize.X / 3, gridSize.Y / 3);

    GD.Print($"DropCircle: gridPos={gridPos}, gridSize={gridSize}, cellSize={cellSize}, dropPosition={dropPosition}");

    if (new Rect2(gridPos, gridSize).HasPoint(dropPosition))
    {
        Vector2 circleCenter = dropPosition - dragOffset + (draggedCircle.Size / 2);
        int row = (int)((circleCenter.Y - gridPos.Y) / cellSize.Y);
        int col = (int)((circleCenter.X - gridPos.X) / cellSize.X);

        GD.Print($"DropCircle: circleCenter={circleCenter}, row={row}, col={col}, draggedCircle.Size={draggedCircle.Size}");

        if (row >= 0 && row < 3 && col >= 0 && col < 3)
        {
            string existingCircleName = board[row, col];
            int draggedSize = GetCircleSize(draggedCircle.Name);

            if (existingCircleName == "" || CanOverride(existingCircleName, draggedSize))
            {
                if (existingCircleName != "")
                {
                    TextureRect existingCircle = FindCircleByName(existingCircleName); // Обновлено на TextureRect
                    if (existingCircle != null)
                    {
                        existingCircle.GetParent().RemoveChild(existingCircle);
                        existingCircle.QueueFree();
                        GD.Print($"Removed {existingCircleName} from ({row}, {col})");
                    }
                }

                Vector2 cellTopLeft = gridPos + new Vector2(col * cellSize.X, row * cellSize.Y);
                Vector2 centeredPosition = cellTopLeft + (cellSize - draggedCircle.Size) / 2;

                draggedCircle.Position = centeredPosition;
                board[row, col] = draggedCircle.Name;
                GD.Print($"{currentPlayer} placed {draggedCircle.Name} at ({row}, {col}), Position={draggedCircle.Position}");

                CheckForWinOrDraw();
                if (!gameEnded)
                {
                    if (gameMode == "bot" && currentPlayer == "Player1")
                    {
                        currentPlayer = "Player2";
                        ui.UpdateStatus("Ход бота");
                        isBotThinking = true;
                        StartBotMoveWithDelay();
                    }
                    else
                    {
                        currentPlayer = currentPlayer == "Player1" ? "Player2" : "Player1";
                        string currentNickname = GetCurrentPlayerNickname();
                        ui.UpdateStatus($"Ход {currentNickname}");
                        GD.Print($"Updated status: Ход {currentNickname}");
                    }
                }
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
            GD.Print($"DropCircle: Invalid cell (row={row}, col={col}), resetting position");
        }
    }
    else
    {
        ResetCirclePosition(draggedCircle);
        GD.Print($"DropCircle: Drop position {dropPosition} outside grid, resetting position");
    }
}

    private async void StartBotMoveWithDelay()
    {
        GD.Print("Бот думает...");
        await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
        BotMove();
        draggedCircle = null;
        dragOffset = Vector2.Zero;
        isBotThinking = false;
        if (!gameEnded)
        {
            currentPlayer = "Player1";
            string currentNickname = GetCurrentPlayerNickname();
            ui.UpdateStatus($"Ход {currentNickname}");
            GD.Print($"Updated status after bot move: Ход {currentNickname}");
        }
    }

    private void BotMove()
{
    var move = botAI?.GetMove();
    if (move.HasValue)
    {
        (TextureRect botCircle, Vector2I targetCell) = move.Value; // Обновлено на TextureRect
        draggedCircle = botCircle;
        dragOffset = botCircle.Size / 2;
        botCircle.GetParent().RemoveChild(botCircle);
        AddChild(botCircle);

        Vector2 gridPos = grid.GlobalPosition;
        Vector2 cellSize = new Vector2(grid.Size.X / 3, grid.Size.Y / 3);
        Vector2 dropPos = gridPos + new Vector2(targetCell.Y * cellSize.X, targetCell.X * cellSize.Y) + cellSize / 2;
        DropCircle(dropPos);
        GD.Print($"Бот выбрал {botCircle.Name} для ячейки ({targetCell.X}, {targetCell.Y})");
    }
    else
    {
        GD.Print("Бот не может сделать ход, переход хода к игроку 1");
        currentPlayer = "Player1";
        string currentNickname = GetCurrentPlayerNickname();
        ui.UpdateStatus($"Ход {currentNickname}");
        GD.Print($"Updated status after bot failed to move: Ход {currentNickname}");
    }
}

    private void CheckForWinOrDraw()
    {
        var global = GetNode<Global>("/root/Global");
        string winner = CheckForWin();
        if (winner != null)
        {
            gameEnded = true;
            string winnerNickname = winner == "Player1" ? global.PlayerNickname : 
                                   (gameMode == "bot" ? "Bot" : global.PlayerNickname + "_friend");
            ui.UpdateStatus($"{winnerNickname} победил!");
            GD.Print($"{winnerNickname} wins!");
            return;
        }

        if (IsBoardFull())
        {
            gameEnded = true;
            ui.UpdateStatus("Ничья!");
            GD.Print("Game ended in a draw!");
        }
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

    private TextureRect FindCircleByName(string name) // Обновлено на TextureRect
{
    foreach (Node child in GetChildren())
    {
        TextureRect circle = child as TextureRect; // Обновлено на TextureRect
        if (circle != null && circle.Name == name)
            return circle;
    }
    return null;
}

    private void ResetCirclePosition(TextureRect circle) // Обновлено на TextureRect
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

    public void ResetGame()
    {
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                board[i, j] = "";

        ClearCirclesFrom(GetChildren());
        ClearCirclesFrom(player1Table.GetChildren());
        ClearCirclesFrom(player2Table.GetChildren());

        initialPositions.Clear();
        initialParents.Clear();

        CreateCircles(player1Table, "P1");
        CreateCircles(player2Table, "P2");

        currentPlayer = "Player1";
        gameEnded = false;
        isBotThinking = false;
        draggedCircle = null;
        dragOffset = Vector2.Zero;
        string currentNickname = GetCurrentPlayerNickname();
        ui.UpdateStatus($"Ход {currentNickname}");
        GD.Print($"Game reset with all circles recreated! Updated status: Ход {currentNickname}");
    }

    private void ClearCirclesFrom(Godot.Collections.Array<Godot.Node> children)
{
    foreach (Node child in children)
    {
        TextureRect circle = child as TextureRect; // Обновлено на TextureRect
        if (circle != null)
        {
            circle.GetParent().RemoveChild(circle);
            circle.QueueFree();
        }
    }
}

    private void CreateGameField()
    {
        grid.Columns = 3;
        gridButtons = new Button[9];

        StyleBoxFlat gridStyle = new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0)
        };
        grid.AddThemeStyleboxOverride("panel", gridStyle);
        grid.Modulate = new Color(1, 1, 1, 1);

        grid.AddThemeConstantOverride("h_separation", 12);
        grid.AddThemeConstantOverride("v_separation", 12);
        int CornerRadius = 12;

        normalStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.098f, 0.463f, 0.824f, 1.0f),
            CornerRadiusTopLeft = CornerRadius,
            CornerRadiusTopRight = CornerRadius,
            CornerRadiusBottomRight = CornerRadius,
            CornerRadiusBottomLeft = CornerRadius,
            ShadowSize = 4,
            ShadowOffset = new Vector2(4, 4),
            ShadowColor = new Color(0, 0, 0, 0.5f)
        };

        highlightStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.114f, 0.486f, 0.863f, 1.0f),
            CornerRadiusTopLeft = CornerRadius,
            CornerRadiusTopRight = CornerRadius,
            CornerRadiusBottomRight = CornerRadius,
            CornerRadiusBottomLeft = CornerRadius,
            ShadowSize = 4,
            ShadowOffset = new Vector2(4, 4),
            ShadowColor = new Color(0, 0, 0, 0.5f)
        };

        StyleBoxEmpty emptyStyle = new StyleBoxEmpty();

        for (int i = 0; i < 9; i++)
        {
            Button button = new Button
            {
                Name = "Button" + i,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                Disabled = false,
                Visible = true,
                Flat = false,
                MouseFilter = Control.MouseFilterEnum.Stop
            };
            button.AddThemeFontSizeOverride("font_size", CalculateFontSize());

            button.AddThemeStyleboxOverride("normal", normalStyle);
            button.AddThemeStyleboxOverride("hover", highlightStyle);
            button.AddThemeStyleboxOverride("pressed", emptyStyle);
            button.AddThemeStyleboxOverride("disabled", normalStyle);

            button.MouseEntered += () => GD.Print($"Mouse entered Button{i}");
            button.MouseExited += () => GD.Print($"Mouse exited Button{i}");

            grid.AddChild(button);
            gridButtons[i] = button;
            GD.Print($"Added Button{i} to Grid, Button Visible: {button.Visible}, Grid Size: {grid.Size}");
        }
    }

    private void CreateCircles(Control table, string playerPrefix)
{
    float tableWidth = table.Size.X;
    float tableHeight = table.Size.Y;
    GD.Print($"{table.Name} Width: {tableWidth}, Height: {tableHeight}");

    bool isMobile = OS.GetName() == "Android" || OS.GetName() == "iOS";
    float baseSize = isMobile ? tableHeight * 0.8f : Mathf.Min((tableWidth / 2) * 0.9f, (tableHeight / 3) * 0.9f);
    GD.Print($"Base size for {table.Name}: {baseSize}");

    // Определяем размеры кругов
    Vector2[] sizes = 
    {
        new Vector2(baseSize * 0.7f, baseSize * 0.7f), // Маленькие круги
        new Vector2(baseSize * 0.85f, baseSize * 0.85f), // Средние круги
        new Vector2(baseSize, baseSize) // Большие круги
    };
    string[] sizeNames = { "Small", "Medium", "Large" };

    // Логируем размеры кругов
    for (int i = 0; i < sizes.Length; i++)
    {
        GD.Print($"{sizeNames[i]} circle size: {sizes[i]}");
    }

    // Вычисляем расстояние между кругами
    float spacingX = Mathf.Min((tableWidth - (sizes[0].X * 2)) / 3, 10f);
    float spacingY = Mathf.Min((tableHeight - (sizes[0].Y + sizes[1].Y + sizes[2].Y)) / 4, 10f);
    if (spacingX < 2f) spacingX = 2f;
    if (spacingY < 2f) spacingY = 2f;
    GD.Print($"Spacing for {table.Name}: X={spacingX}, Y={spacingY}");

    // Цвета в стиле Google Material Design
    Color circleColor = playerPrefix == "P1" ? new Color("#F44336") : new Color("#2196F3"); // Red 500 для P1, Blue 500 для P2

    // Загружаем текстуру круга
    var circleTexture = GD.Load<Texture2D>("res://Sprites/circle.png");
    if (circleTexture == null)
    {
        GD.PrintErr($"Failed to load circle texture at res://Assets/circle.png for {table.Name}");
        return;
    }

    // Создаём шейдер для изменения цвета
    Shader shader = GD.Load<Shader>("res://Styles/circle_color.gdshader");
    if (shader == null)
    {
        GD.PrintErr($"Failed to load shader at res://Shaders/circle_color.gdshader for {table.Name}");
        return;
    }

    ShaderMaterial shaderMaterial = new ShaderMaterial();
    shaderMaterial.Shader = shader;
    shaderMaterial.SetShaderParameter("circle_color", circleColor);

    float currentY = spacingY;
    for (int row = 0; row < 3; row++)
    {
        int sizeIdx = row;
        float columnWidth = tableWidth / 2;
        float leftCircleX = (columnWidth - sizes[sizeIdx].X) / 2;
        float rightCircleX = columnWidth + (columnWidth - sizes[sizeIdx].X) / 2;

        // Первый круг (левый)
        TextureRect circle1 = new TextureRect
        {
            Name = $"{playerPrefix}_{sizeNames[sizeIdx]}Circle1",
            Size = sizes[sizeIdx],
            Position = new Vector2(leftCircleX, currentY),
            Texture = circleTexture,
            Material = shaderMaterial, // Применяем шейдер для цвета
            MouseFilter = Control.MouseFilterEnum.Stop,
            Visible = true,
            ShowBehindParent = false,
            UseParentMaterial = false,
            Modulate = new Color(1, 1, 1, 1) // Убедимся, что текстура не затемнена
        };
        // Добавляем тень
        circle1.Set("material/shadow_enabled", true);
        circle1.Set("material/shadow_color", new Color(0, 0, 0, 0.3f));
        circle1.Set("material/shadow_offset", new Vector2(2, 2));
        table.AddChild(circle1);
        initialPositions[circle1.Name] = circle1.Position;
        initialParents[circle1.Name] = table;
        GD.Print($"Created {circle1.Name} at {circle1.Position}, size: {circle1.Size}, Visible: {circle1.Visible}");

        // Второй круг (правый)
        TextureRect circle2 = new TextureRect
        {
            Name = $"{playerPrefix}_{sizeNames[sizeIdx]}Circle2",
            Size = sizes[sizeIdx],
            Position = new Vector2(rightCircleX, currentY),
            Texture = circleTexture,
            Material = shaderMaterial.Duplicate() as ShaderMaterial, // Дублируем материал
            MouseFilter = Control.MouseFilterEnum.Stop,
            Visible = true,
            Modulate = new Color(1, 1, 1, 1)
        };
        // Применяем цвет к дублированному материалу
        if (circle2.Material is ShaderMaterial circle2Material)
        {
            circle2Material.SetShaderParameter("circle_color", circleColor);
        }
        else
        {
            GD.PrintErr($"Failed to cast circle2.Material to ShaderMaterial for {circle2.Name}");
        }
        // Добавляем тень
        circle2.Set("material/shadow_enabled", true);
        circle2.Set("material/shadow_color", new Color(0, 0, 0, 0.3f));
        circle2.Set("material/shadow_offset", new Vector2(2, 2));
        table.AddChild(circle2);
        initialPositions[circle2.Name] = circle2.Position;
        initialParents[circle2.Name] = table;
        GD.Print($"Created {circle2.Name} at {circle2.Position}, size: {circle2.Size}, Visible: {circle2.Visible}");

        currentY += sizes[sizeIdx].Y + spacingY;
    }
}

    private int CalculateFontSize()
    {
        Vector2 screenSize = GetViewport().GetVisibleRect().Size;
        return Mathf.Clamp((int)(screenSize.Y / 20f), 24, 64);
    }
}