using Godot;
using System.Collections.Generic;

public partial class SinglePlayerGame : Node2D
{
    private string[,] board = new string[3, 3];
    private string currentPlayer = "Player1";
    private GridContainer grid;
    private Control player1Table;
    private Control player2Table;
    private Label player1Label; // Метка для Player1
    private Label player2Label; // Метка для Player2
    private UI ui;
    private bool gameEnded = false;
    private TextureRect draggedCircle = null;
    private Vector2 dragOffset = Vector2.Zero;
    private Dictionary<string, Vector2> initialPositions = new Dictionary<string, Vector2>();
    private Dictionary<string, Node> initialParents = new Dictionary<string, Node>();
    private Button highlightedButton = null;
    private Button[] gridButtons;
    private string gameMode = "friend";
    private Button backToMenuButton;
    private Button restartButton;
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
        player1Table = GetNode<Control>("Player1Table");
        player2Table = GetNode<Control>("Player2Table");
        player1Label = GetNode<Label>("Player1Table/Player1Label");
        player2Label = GetNode<Label>("Player2Table/Player2Label");
        ui = GetNode<UI>("UI");
        backToMenuButton = GetNode<Button>("UI/BackToMenuButton");
        restartButton = GetNode<Button>("UI/RestartButton");

        // Проверка на null с отладкой
        if (grid == null || player1Table == null || player2Table == null || 
            player1Label == null || player2Label == null || ui == null || 
            backToMenuButton == null || restartButton == null)
        {
            GD.PrintErr("Ошибка: Один из узлов не найден!");
            GD.Print($"grid: {grid}, player1Table: {player1Table}, player2Table: {player2Table}, " +
                     $"player1Label: {player1Label}, player2Label: {player2Label}, ui: {ui}, " +
                     $"backToMenuButton: {backToMenuButton}, restartButton: {restartButton}");
            return;
        }

        var global = GetNode<Global>("/root/Global");
        gameMode = global.GameMode;

        // Устанавливаем никнеймы с отладкой
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

        CreateCircles(player1Table, "P1");
        CreateCircles(player2Table, "P2");

        string initialNickname = currentPlayer == "Player1" ? global.PlayerNickname : 
                                (gameMode == "bot" ? "Bot" : global.PlayerNickname + "_friend");
        ui.UpdateStatus($"Ход {initialNickname}");
        GD.Print($"Initial status: Ход {initialNickname}");

        backToMenuButton.Pressed += ui.OnMenuButtonPressed;
        restartButton.Pressed += ui.OnRestartButtonPressed;
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

    private void DropCircle(Vector2 dropPosition)
    {
        Vector2 gridPos = grid.GlobalPosition;
        Vector2 gridSize = grid.Size;
        Vector2 cellSize = new Vector2(gridSize.X / 3, grid.Size.Y / 3);

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

    private int CalculateFontSize()
    {
        Vector2 screenSize = GetViewport().GetVisibleRect().Size;
        return Mathf.Clamp((int)(screenSize.X / 10f), 24, 64);
    }
}