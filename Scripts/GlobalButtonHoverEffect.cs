using Godot;
using System.Collections.Generic;

public partial class GlobalButtonHoverEffect : Node
{
    private List<Button> buttons = new List<Button>();
    private const float ScaleIncrease = 1.1f;
    private const float AnimationDuration = 0.2f;

    public override void _Ready()
    {
        //GD.Print("GlobalButtonHoverEffect: Loaded and searching for buttons");
        FindButtons(GetTree().Root);
        //GD.Print($"GlobalButtonHoverEffect: Found {buttons.Count} buttons initially");
        GetTree().NodeAdded += OnNodeAdded;
    }

    private void FindButtons(Node node)
    {
        if (node is Button button)
        {
            //GD.Print($"GlobalButtonHoverEffect: Found button {button.Name} at path {button.GetPath()}");
            AddHoverEffect(button);
            buttons.Add(button);
        }

        foreach (Node child in node.GetChildren())
        {
            FindButtons(child);
        }
    }

    private void OnNodeAdded(Node node)
    {
        if (node is Button button && !buttons.Contains(button))
        {
            //GD.Print($"GlobalButtonHoverEffect: New button added dynamically: {button.Name} at path {button.GetPath()}");
            AddHoverEffect(button);
            buttons.Add(button);
        }
    }

    private void AddHoverEffect(Button button)
    {
        button.PivotOffset = button.Size / 2;

        button.Resized += () =>
        {
            button.PivotOffset = button.Size / 2;
        };

        Vector2 originalScale = button.Scale;

        button.MouseEntered += () =>
        {
            //GD.Print($"Mouse entered on button: {button.Name}");
            Tween tween = button.CreateTween();
            tween.TweenProperty(button, "scale", originalScale * ScaleIncrease, AnimationDuration)
                 .SetTrans(Tween.TransitionType.Sine)
                 .SetEase(Tween.EaseType.Out);
        };

        button.MouseExited += () =>
        {
            //GD.Print($"Mouse exited on button: {button.Name}");
            Tween tween = button.CreateTween();
            tween.TweenProperty(button, "scale", originalScale, AnimationDuration)
                 .SetTrans(Tween.TransitionType.Sine)
                 .SetEase(Tween.EaseType.Out);
        };
    }

    public override void _ExitTree()
    {
        GetTree().NodeAdded -= OnNodeAdded;
    }
}