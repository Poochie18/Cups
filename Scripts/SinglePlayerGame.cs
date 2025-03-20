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

        // Определяем устройство и настраиваем расположение
        SetupDeviceLayout();

        CreateCircles(player1Table, "P1");
        CreateCircles(player2Table, "P2");

        string initialNickname = currentPlayer == "Player1" ? global.PlayerNickname : 
                                (gameMode == "bot" ? "Bot" : global.PlayerNickname + "_friend");
        ui.UpdateStatus($"Ход {initialNickname}");
        GD.Print($"Initial status: Ход {initialNickname}");

        ui.GetNode<Button>("RestartButton").Pressed += ui.OnRestartButtonPressed;
        ui.GetNode<Button>("BackToMenuButton").Pressed += ui.OnMenuButtonPressed;
    }

    private void SetupDeviceLayout()
{
    bool isMobile = OS.GetName() == "Android" || OS.GetName() == "iOS";
    Vector2 screenSize = GetViewport().GetVisibleRect().Size;

    if (isMobile)
    {
        // Портретный режим для смартфона (занимаемся позже)
        DisplayServer.WindowSetSize(new Vector2I((int)screenSize.X, (int)screenSize.Y));
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Maximized);

        ui.Position = new Vector2((screenSize.X - ui.Size.X) / 2, 20);
        GD.Print($"UI Position: {ui.Position}, Size: {ui.Size}");

        var player1TableContainer = GetNode<Control>("Player1TableContainer");
        player1TableContainer.Position = new Vector2((screenSize.X - player1TableContainer.Size.X) / 2, ui.Position.Y + ui.Size.Y + 20);
        player1Label.Position = new Vector2((player1TableContainer.Size.X - player1Label.Size.X) / 2, 0);
        player1Table.Position = new Vector2((player1TableContainer.Size.X - player1Table.Size.X) / 2, player1Label.Size.Y + 10);
        GD.Print($"Player1TableContainer Position: {player1TableContainer.Position}, Size: {player1TableContainer.Size}");

        grid.Position = new Vector2(20, player1TableContainer.Position.Y + player1TableContainer.Size.Y + 20);
        grid.Size = new Vector2(screenSize.X - 40, screenSize.X - 40);
        GD.Print($"Grid Position: {grid.Position}, Size: {grid.Size}");

        var player2TableContainer = GetNode<Control>("Player2TableContainer");
        player2TableContainer.Position = new Vector2((screenSize.X - player2TableContainer.Size.X) / 2, grid.Position.Y + grid.Size.Y + 20);
        player2Label.Position = new Vector2((player2TableContainer.Size.X - player2Label.Size.X) / 2, 0);
        player2Table.Position = new Vector2((player2TableContainer.Size.X - player2Table.Size.X) / 2, player2Label.Size.Y + 10);
        GD.Print($"Player2TableContainer Position: {player2TableContainer.Position}, Size: {player2TableContainer.Size}");
    }
    else
    {
        // Горизонтальный режим для ПК (16:9, 1280x720)
        DisplayServer.WindowSetSize(new Vector2I(1280, 720));
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        screenSize = new Vector2(1280, 720);
        GD.Print($"Screen Size: {screenSize}");

        // UI сверху
        ui.Size = new Vector2(screenSize.X, 100);
        ui.Position = new Vector2(0, 0);
        ui.Visible = true;

        // Настройка дочерних элементов UI
        var restartButton = ui.GetNode<Button>("RestartButton");
        var statusLabel = ui.GetNode<Label>("StatusLabel");
        var backToMenuButton = ui.GetNode<Button>("BackToMenuButton");

        restartButton.Size = new Vector2(150, 50);
        restartButton.Position = new Vector2(20, 25);
        restartButton.Visible = true;

        statusLabel.Size = new Vector2(300, 50);
        statusLabel.Position = new Vector2((screenSize.X - statusLabel.Size.X) / 2, 25);
        statusLabel.Visible = true;

        backToMenuButton.Size = new Vector2(150, 50);
        backToMenuButton.Position = new Vector2(screenSize.X - backToMenuButton.Size.X - 20, 25);
        backToMenuButton.Visible = true;

        GD.Print($"UI Position: {ui.Position}, Size: {ui.Size}");
        GD.Print($"RestartButton Position: {restartButton.Position}, Size: {restartButton.Size}");
        GD.Print($"StatusLabel Position: {statusLabel.Position}, Size: {statusLabel.Size}");
        GD.Print($"BackToMenuButton Position: {backToMenuButton.Position}, Size: {backToMenuButton.Size}");

        // Grid по центру
        float gridSize = 500;
        grid.Position = new Vector2((screenSize.X - gridSize) / 2, ui.Size.Y + 20);
        grid.Size = new Vector2(gridSize, gridSize);
        grid.Visible = true;
        GD.Print($"Grid Position: {grid.Position}, Size: {grid.Size}");

        // Player1TableContainer справа
        var player1TableContainer = GetNode<Control>("Player1TableContainer");
        float player1TableWidth = (screenSize.X - gridSize) / 2 - 40;
        float player1TableHeight = gridSize;
        player1TableContainer.Position = new Vector2(grid.Position.X + gridSize + 20, ui.Size.Y + 20);
        player1TableContainer.Size = new Vector2(player1TableWidth, player1TableHeight);
        player1TableContainer.Visible = true;
        player1Label.Position = new Vector2((player1TableWidth - player1Label.Size.X) / 2, 0);
        player1Table.Position = new Vector2(0, player1Label.Size.Y + 10);
        player1Table.Size = new Vector2(player1TableWidth, player1TableHeight - player1Label.Size.Y - 10);
        player1Table.Visible = true;
        GD.Print($"Player1TableContainer Position: {player1TableContainer.Position}, Size: {player1TableContainer.Size}");
        GD.Print($"Player1Table Position: {player1Table.Position}, Size: {player1Table.Size}");

        // Player2TableContainer слева
        var player2TableContainer = GetNode<Control>("Player2TableContainer");
        float player2TableWidth = (screenSize.X - gridSize) / 2 - 40;
        float player2TableHeight = gridSize;
        player2TableContainer.Position = new Vector2(20, ui.Size.Y + 20);
        player2TableContainer.Size = new Vector2(player2TableWidth, player2TableHeight);
        player2TableContainer.Visible = true;
        player2Label.Position = new Vector2((player2TableWidth - player2Label.Size.X) / 2, 0);
        player2Table.Position = new Vector2(0, player2Label.Size.Y + 10);
        player2Table.Size = new Vector2(player2TableWidth, player2TableHeight - player2Label.Size.Y - 10);
        player2Table.Visible = true;
        GD.Print($"Player2TableContainer Position: {player2TableContainer.Position}, Size: {player2TableContainer.Size}");
        GD.Print($"Player2Table Position: {player2Table.Position}, Size: {player2Table.Size}");
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
            TextureRect circle = child as TextureRect;
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

    private void DropCircle(Vector2 dropPosition)
    {
        Vector2 gridPos = grid.GlobalPosition;
        Vector2 gridSize = grid.Size;
        Vector2 cellSize = new Vector2(gridSize.X / 3, gridSize.Y / 3);

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

                    CheckForWinOrDraw();
                    if (!gameEnded)
                    {
                        var global = GetNode<Global>("/root/Global");
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
                            string currentNickname = currentPlayer == "Player1" ? global.PlayerNickname : 
                                                    (gameMode == "bot" ? "Bot" : global.PlayerNickname + "_friend");
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
            }
        }
        else
        {
            ResetCirclePosition(draggedCircle);
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
            ui.UpdateStatus("Ход игрока 1");
        }
    }

    private void BotMove()
    {
        var move = botAI?.GetMove();
        if (move.HasValue)
        {
            (TextureRect botCircle, Vector2I targetCell) = move.Value;
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
            ui.UpdateStatus("Ход игрока 1");
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
        ui.UpdateStatus("Ход игрока 1");

        GD.Print("Game reset with all circles recreated!");
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

    private void CreateCircles(Control table, string playerPrefix)
{
    Texture2D texture = GD.Load<Texture2D>("res://Sprites/icon.svg") ?? GD.Load<Texture2D>("res://icon.png");
    if (texture == null)
    {
        GD.PrintErr("Не удалось загрузить текстуру для кругляшек!");
        return;
    }

    // Получаем размеры PlayerTable
    float tableWidth = table.Size.X;
    float tableHeight = table.Size.Y;
    GD.Print($"{table.Name} Width: {tableWidth}, Height: {tableHeight}");

    // Определяем устройство
    bool isMobile = OS.GetName() == "Android" || OS.GetName() == "iOS";

    // Базовый размер кругляшка (Large) зависит от устройства
    float baseSize;
    if (isMobile)
    {
        baseSize = tableHeight * 0.8f;
    }
    else
    {
        // Ограничиваем размер по ширине (2 кругляшка в ряду) и высоте (3 ряда)
        float maxWidth = (tableWidth / 2) * 0.9f; // 90% от ширины столбца
        float maxHeight = (tableHeight / 3) * 0.9f; // 90% от высоты ряда
        baseSize = Mathf.Min(maxWidth, maxHeight); // Выбираем меньшее значение
    }

    Vector2[] sizes = 
    {
        new Vector2(baseSize * 0.5f, baseSize * 0.5f), // Small
        new Vector2(baseSize * 0.75f, baseSize * 0.75f), // Medium
        new Vector2(baseSize, baseSize) // Large
    };
    string[] sizeNames = { "Small", "Medium", "Large" };

    // Уменьшаем расстояние между кругляшками
    float spacingX = (tableWidth - (sizes[0].X * 2)) / 3; // 3 промежутка (слева, между кругляшками, справа)
    float spacingY = (tableHeight - (sizes[0].Y + sizes[1].Y + sizes[2].Y)) / 4; // 4 промежутка (сверху, между рядами, снизу)
    spacingX = Mathf.Min(spacingX, 10f); // Ограничиваем максимальный отступ по горизонтали
    spacingY = Mathf.Min(spacingY, 10f); // Ограничиваем максимальный отступ по вертикали
    if (spacingX < 2f) spacingX = 2f; // Минимальный отступ
    if (spacingY < 2f) spacingY = 2f;

    // Позиции для каждого ряда
    float currentY = spacingY;
    for (int row = 0; row < 3; row++)
    {
        int sizeIdx = row; // Small в первом ряду, Medium во втором, Large в третьем
        // Центрируем кругляшки по горизонтали относительно их столбца
        float columnWidth = tableWidth / 2; // Делим стол на 2 столбца
        float leftCircleX = (columnWidth - sizes[sizeIdx].X) / 2; // Центрируем в первом столбце
        float rightCircleX = columnWidth + (columnWidth - sizes[sizeIdx].X) / 2; // Центрируем во втором столбце

        // Первый кругляш в ряду (левый столбец)
        TextureRect circle1 = new TextureRect
        {
            Name = $"{playerPrefix}_{sizeNames[sizeIdx]}Circle1",
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            Size = sizes[sizeIdx],
            Position = new Vector2(leftCircleX, currentY),
            MouseFilter = Control.MouseFilterEnum.Stop,
            Modulate = playerPrefix == "P1" ? new Color(1, 0, 0) : new Color(0, 0, 1),
            Visible = true
        };
        table.AddChild(circle1);
        initialPositions[circle1.Name] = circle1.Position;
        initialParents[circle1.Name] = table;
        GD.Print($"Created {circle1.Name} at {circle1.Position}, size: {circle1.Size}, Visible: {circle1.Visible}");

        // Второй кругляш в ряду (правый столбец)
        TextureRect circle2 = new TextureRect
        {
            Name = $"{playerPrefix}_{sizeNames[sizeIdx]}Circle2",
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            Size = sizes[sizeIdx],
            Position = new Vector2(rightCircleX, currentY),
            MouseFilter = Control.MouseFilterEnum.Stop,
            Modulate = playerPrefix == "P1" ? new Color(1, 0, 0) : new Color(0, 0, 1),
            Visible = true
        };
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