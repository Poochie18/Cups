using System;
using System.Collections.Generic;
using Godot;

public class BotAI
{
    private string[,] board;
    private Control playerTable;
    private Control opponentTable;
    private int difficulty;

    private static readonly int[,] WinningCombinations = new int[,]
    {
        {0, 1, 2}, {3, 4, 5}, {6, 7, 8},
        {0, 3, 6}, {1, 4, 7}, {2, 5, 8},
        {0, 4, 8}, {2, 4, 6}
    };

    public BotAI(string[,] board, Control playerTable, Control opponentTable, int difficulty)
    {
        this.board = board;
        this.playerTable = playerTable;
        this.opponentTable = opponentTable;
        this.difficulty = difficulty;

        //if (playerTable == null || opponentTable == null)
            //GD.PrintErr($"BotAI: playerTable is {playerTable}, opponentTable is {opponentTable}");
    }

    public (TextureRect, Vector2I)? GetMove() // Updated to TextureRect
    {
        if (playerTable == null || playerTable.GetChildCount() == 0)
        {
            //GD.PrintErr($"BotAI.GetMove: playerTable is null or empty, cups left={playerTable?.GetChildCount() ?? 0}");
            return null;
        }

        //GD.Print($"BotAI.GetMove: Starting move calculation for difficulty {difficulty}, cups left={playerTable.GetChildCount()}");
        return difficulty switch
        {
            0 => EasyMove(),
            1 => MediumMove(),
            2 => HardMove(),
            _ => EasyMove()
        };
    }

    private (TextureRect, Vector2I)? EasyMove() // Updated to TextureRect
    {
        var (freeCells, overwriteCells) = GetAvailableCells();
        //GD.Print($"BotAI.EasyMove: freeCells={freeCells.Count}, overwriteCells={overwriteCells.Count}");
        if (freeCells.Count == 0 && overwriteCells.Count == 0)
        {
            //GD.Print("BotAI.EasyMove: No available moves!");
            return null;
        }

        Random rand = new Random();
        if (overwriteCells.Count > 0 && rand.Next(3) != 0)
        {
            Vector2I cell = overwriteCells[rand.Next(overwriteCells.Count)];
            int existingSize = GetCircleSize(board[cell.X, cell.Y]);
            TextureRect circle = GetAnyAvailableCircle(existingSize + 1); // Updated to TextureRect
            if (circle != null)
            {
                //GD.Print($"BotAI.EasyMove: Overwriting at ({cell.X}, {cell.Y}) with {circle.Name}");
                return (circle, cell);
            }
        }
        else if (freeCells.Count > 0)
        {
            Vector2I cell = freeCells[rand.Next(freeCells.Count)];
            TextureRect circle = GetAnyAvailableCircle(0); // Updated to TextureRect
            if (circle != null)
            {
                //GD.Print($"BotAI.EasyMove: Placing {circle.Name} at ({cell.X}, {cell.Y})");
                return (circle, cell);
            }
        }

        //GD.Print("BotAI.EasyMove: No suitable circle found!");
        return null;
    }

    private (TextureRect, Vector2I)? MediumMove() // Updated to TextureRect
    {
        var (freeCells, overwriteCells) = GetAvailableCells();
        //GD.Print($"BotAI.MediumMove: freeCells={freeCells.Count}, overwriteCells={overwriteCells.Count}");
        if (freeCells.Count == 0 && overwriteCells.Count == 0)
        {
            //GD.Print("BotAI.MediumMove: No available moves, forcing a move if possible");
            return ForceMove();
        }

        var winMove = FindBestMove(freeCells, overwriteCells, "P2");
        if (winMove.HasValue)
            return winMove;

        var blockMove = FindBestMove(freeCells, overwriteCells, "P1");
        if (blockMove.HasValue)
            return blockMove;

        if (overwriteCells.Count > 0)
        {
            Vector2I cell = overwriteCells[0];
            int existingSize = GetCircleSize(board[cell.X, cell.Y]);
            TextureRect circle = GetAnyAvailableCircle(existingSize + 1); // Updated to TextureRect
            if (circle != null)
            {
                //GD.Print($"BotAI.MediumMove: Overwriting at ({cell.X}, {cell.Y}) with {circle.Name}");
                return (circle, cell);
            }
        }

        if (freeCells.Contains(new Vector2I(1, 1)))
        {
            TextureRect circle = GetAnyAvailableCircle(0); // Updated to TextureRect
            if (circle != null)
            {
                //GD.Print($"BotAI.MediumMove: Taking center with {circle.Name}");
                return (circle, new Vector2I(1, 1));
            }
        }

        if (freeCells.Count > 0)
        {
            Vector2I cell = freeCells[0];
            TextureRect circle = GetAnyAvailableCircle(0); // Updated to TextureRect
            if (circle != null)
            {
                //GD.Print($"BotAI.MediumMove: Free cell at ({cell.X}, {cell.Y}) with {circle.Name}");
                return (circle, cell);
            }
        }

        //GD.Print("BotAI.MediumMove: No suitable circle found!");
        return null;
    }

    private (TextureRect, Vector2I)? HardMove() // Updated to TextureRect
    {
        var (freeCells, overwriteCells) = GetAvailableCells();
        //GD.Print($"BotAI.HardMove: freeCells={freeCells.Count}, overwriteCells={overwriteCells.Count}");
        if (freeCells.Count == 0 && overwriteCells.Count == 0)
        {
            //GD.Print("BotAI.HardMove: No available moves, forcing a move if possible");
            return ForceMove();
        }

        var winMove = FindBestMove(freeCells, overwriteCells, "P2");
        if (winMove.HasValue)
            return winMove;

        var blockMove = FindBestMove(freeCells, overwriteCells, "P1");
        if (blockMove.HasValue)
            return blockMove;

        if (overwriteCells.Count > 0)
        {
            Vector2I cell = overwriteCells[0];
            int existingSize = GetCircleSize(board[cell.X, cell.Y]);
            TextureRect circle = GetLargestAvailableCircle(); // Updated to TextureRect
            if (circle != null && GetCircleSize(circle.Name) > existingSize)
            {
                //GD.Print($"BotAI.HardMove: Overwriting at ({cell.X}, {cell.Y}) with {circle.Name}");
                return (circle, cell);
            }
        }

        Vector2I[] strategicCells = { new Vector2I(1, 1), new Vector2I(0, 0), new Vector2I(0, 2), new Vector2I(2, 0), new Vector2I(2, 2) };
        foreach (Vector2I cell in strategicCells)
        {
            if (freeCells.Contains(cell))
            {
                TextureRect circle = GetAnyAvailableCircle(0); // Updated to TextureRect
                if (circle != null)
                {
                    //GD.Print($"BotAI.HardMove: Strategic free cell at ({cell.X}, {cell.Y}) with {circle.Name}");
                    return (circle, cell);
                }
            }
        }

        if (freeCells.Count > 0)
        {
            Vector2I cell = freeCells[0];
            TextureRect circle = GetAnyAvailableCircle(0); // Updated to TextureRect
            if (circle != null)
            {
                //GD.Print($"BotAI.HardMove: Free cell at ({cell.X}, {cell.Y}) with {circle.Name}");
                return (circle, cell);
            }
        }

        //GD.Print("BotAI.HardMove: No suitable circle found!");
        return null;
    }

    private (List<Vector2I> freeCells, List<Vector2I> overwriteCells) GetAvailableCells()
    {
        List<Vector2I> freeCells = new List<Vector2I>();
        List<Vector2I> overwriteCells = new List<Vector2I>();
        for (int i = 0; i < 9; i++)
        {
            int row = i / 3;
            int col = i % 3;
            string cell = board[row, col];
            Vector2I position = new Vector2I(row, col);

            if (string.IsNullOrEmpty(cell))
            {
                freeCells.Add(position);
            }
            else if (cell.StartsWith("P1") && CanOverride(cell))
            {
                overwriteCells.Add(position);
            }
        }
        return (freeCells, overwriteCells);
    }

    private (TextureRect, Vector2I)? ForceMove() // Updated to TextureRect
    {
        for (int i = 0; i < 9; i++)
        {
            int row = i / 3;
            int col = i % 3;
            string cell = board[row, col];
            foreach (Node child in playerTable.GetChildren())
            {
                if (child is TextureRect cup && CanPlace(GetCircleSize(cup.Name), cell)) // Updated to TextureRect
                {
                    //GD.Print($"BotAI.ForceMove: Forced move at ({row}, {col}) with {cup.Name}");
                    return (cup, new Vector2I(row, col));
                }
            }
        }
        //GD.Print("BotAI.ForceMove: No forced moves available!");
        return null;
    }

    private (TextureRect, Vector2I)? FindBestMove(List<Vector2I> freeCells, List<Vector2I> overwriteCells, string playerPrefix) // Updated to TextureRect
    {
        for (int i = 0; i < WinningCombinations.GetLength(0); i++)
        {
            int a = WinningCombinations[i, 0];
            int b = WinningCombinations[i, 1];
            int c = WinningCombinations[i, 2];
            string cellA = board[a / 3, a % 3];
            string cellB = board[b / 3, b % 3];
            string cellC = board[c / 3, c % 3];

            int playerCount = 0;
            Vector2I? targetCell = null;
            int minSizeNeeded = 0;

            if (cellA.StartsWith(playerPrefix)) playerCount++;
            else if (string.IsNullOrEmpty(cellA)) targetCell = new Vector2I(a / 3, a % 3);
            else if (playerPrefix == "P2" && cellA.StartsWith("P1"))
            {
                int size = GetCircleSize(cellA);
                targetCell = new Vector2I(a / 3, a % 3);
                minSizeNeeded = Math.Max(minSizeNeeded, size + 1);
            }

            if (cellB.StartsWith(playerPrefix)) playerCount++;
            else if (string.IsNullOrEmpty(cellB)) targetCell = new Vector2I(b / 3, b % 3);
            else if (playerPrefix == "P2" && cellB.StartsWith("P1"))
            {
                int size = GetCircleSize(cellB);
                targetCell = new Vector2I(b / 3, b % 3);
                minSizeNeeded = Math.Max(minSizeNeeded, size + 1);
            }

            if (cellC.StartsWith(playerPrefix)) playerCount++;
            else if (string.IsNullOrEmpty(cellC)) targetCell = new Vector2I(c / 3, c % 3);
            else if (playerPrefix == "P2" && cellC.StartsWith("P1"))
            {
                int size = GetCircleSize(cellC);
                targetCell = new Vector2I(c / 3, c % 3);
                minSizeNeeded = Math.Max(minSizeNeeded, size + 1);
            }

            if (playerCount == 2 && targetCell.HasValue)
            {
                if (freeCells.Contains(targetCell.Value) || overwriteCells.Contains(targetCell.Value))
                {
                    TextureRect circle = playerPrefix == "P2" ? GetLargestAvailableCircle() : GetAnyAvailableCircle(minSizeNeeded); // Updated to TextureRect
                    if (circle != null && GetCircleSize(circle.Name) >= minSizeNeeded)
                    {
                        //GD.Print($"BotAI: {(playerPrefix == "P2" ? "Winning" : "Blocking")} move at ({targetCell.Value.X}, {targetCell.Value.Y}) with {circle.Name}");
                        return (circle, targetCell.Value);
                    }
                }
            }
        }
        return null;
    }

    private bool CanOverride(string existingCircleName)
    {
        if (string.IsNullOrEmpty(existingCircleName)) return true;
        int existingSize = GetCircleSize(existingCircleName);
        foreach (Node child in playerTable.GetChildren())
        {
            if (child is TextureRect cup && GetCircleSize(cup.Name) > existingSize) // Updated to TextureRect
                return true;
        }
        return false;
    }

    private bool CanPlace(int size, string existingCell)
    {
        if (string.IsNullOrEmpty(existingCell)) return true;
        int existingSize = GetCircleSize(existingCell);
        return size > existingSize;
    }

    private TextureRect GetAnyAvailableCircle(int minSize) // Updated to TextureRect
    {
        foreach (Node child in playerTable.GetChildren())
        {
            if (child is TextureRect circle && GetCircleSize(circle.Name) >= minSize) // Updated to TextureRect
                return circle;
        }
        //GD.Print($"BotAI.GetAnyAvailableCircle: No circle found with minSize {minSize}");
        return null;
    }

    private TextureRect GetLargestAvailableCircle() // Updated to TextureRect
    {
        TextureRect largest = null; // Updated to TextureRect
        int maxSize = -1;
        foreach (Node child in playerTable.GetChildren())
        {
            if (child is TextureRect circle) // Updated to TextureRect
            {
                int size = GetCircleSize(circle.Name);
                if (size > maxSize)
                {
                    maxSize = size;
                    largest = circle;
                }
            }
        }
        return largest;
    }

    private int GetCircleSize(string circleName)
    {
        if (string.IsNullOrEmpty(circleName)) return 0;
        if (circleName.Contains("Small")) return 1;
        if (circleName.Contains("Medium")) return 2;
        if (circleName.Contains("Large")) return 3;
        return 0;
    }
}