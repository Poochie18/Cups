using Godot;

public partial class Settings : Control
{
    private Button backButton;
    private LineEdit nicknameInput;

    public override void _Ready()
    {
        backButton = GetNode<Button>("SettingsContainer/BackButton");
        nicknameInput = GetNode<LineEdit>("SettingsContainer/NicknameInput");

        if (backButton == null || nicknameInput == null)
        {
            GD.PrintErr("Ошибка: Не найдены узлы в Settings!");
            GD.Print($"backButton: {backButton}, nicknameInput: {nicknameInput}");
            return;
        }

        // Загружаем текущий никнейм из Global, если он есть, иначе оставляем "Player"
        var global = GetNode<Global>("/root/Global");
        nicknameInput.Text = string.IsNullOrEmpty(global.PlayerNickname) ? "Player" : global.PlayerNickname;

        backButton.Pressed += OnBackButtonPressed;
        GD.Print("Settings scene initialized");
    }

    private void OnBackButtonPressed()
    {
        GD.Print("Back button pressed!");
        var global = GetNode<Global>("/root/Global");
        global.PlayerNickname = nicknameInput.Text; // Сохраняем никнейм в Global
        LoadScene("res://Scenes/Menu.tscn");
    }

    private void LoadScene(string path)
    {
        GD.Print($"Attempting to load scene: {path}");
        PackedScene scene = GD.Load<PackedScene>(path);
        if (scene != null)
        {
            Node sceneInstance = scene.Instantiate();
            GetTree().Root.AddChild(sceneInstance);
            GD.Print($"Scene {path} loaded and added to tree");
            QueueFree();
        }
        else
        {
            GD.PrintErr($"Ошибка: Не удалось загрузить сцену {path}!");
        }
    }
}