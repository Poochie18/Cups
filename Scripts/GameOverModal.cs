using Godot;

public partial class GameOverModal : Control
{
    [Export] public Label ResultLabel;
    [Export] public Label InstructionLabel; // Новая метка
    [Export] public Button MenuButton;
    [Export] public Button RestartButton;

    public override void _Ready()
    {
        if (ResultLabel == null || InstructionLabel == null || MenuButton == null || RestartButton == null)
        {
            GD.PrintErr("Ошибка: Один из узлов GameOverModal не найден!");
            GD.Print($"ResultLabel: {ResultLabel}, InstructionLabel: {InstructionLabel}, MenuButton: {MenuButton}, RestartButton: {RestartButton}");
            return;
        }
        InstructionLabel.Text = "Для ещё одной игры нажмите Рестарт"; // Устанавливаем текст по умолчанию
    }
}