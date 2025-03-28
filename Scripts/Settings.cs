using Godot;

public partial class Settings : Control
{
    private Button backButton;
    private LineEdit nicknameInput;
    private Button toggleFullscreenButton; // Новая кнопка для переключения режима
    private DisplayManager displayManager;

    public override void _Ready()
    {
        backButton = GetNode<Button>("SettingsContainer/BackButton");
        nicknameInput = GetNode<LineEdit>("SettingsContainer/NicknameInput");
        toggleFullscreenButton = GetNode<Button>("SettingsContainer/ToggleFullscreenButton"); // Получаем кнопку
        displayManager = GetNode<DisplayManager>("/root/DisplayManager");

        if (backButton == null || nicknameInput == null || toggleFullscreenButton == null || displayManager == null)
        {
            GD.PrintErr("Ошибка: Не найдены узлы в Settings!");
            GD.Print($"backButton: {backButton}, nicknameInput: {nicknameInput}, toggleFullscreenButton: {toggleFullscreenButton}, displayManager: {displayManager}");
            return;
        }

        var global = GetNode<Global>("/root/Global");
        nicknameInput.Text = string.IsNullOrEmpty(global.PlayerNickname) ? "Player" : global.PlayerNickname;

        // Устанавливаем начальный текст кнопки в зависимости от текущего режима
        UpdateFullscreenButtonText();

        // Подключаем обработчик для кнопки
        toggleFullscreenButton.Pressed += OnToggleFullscreenButtonPressed;

        backButton.Pressed += () =>
        {
            GD.Print("BackButton pressed");
            OnBackButtonPressed();
        };
        GD.Print("Settings scene initialized");
    }

    private void UpdateFullscreenButtonText()
    {
        bool isFullscreen = displayManager.GetWindowMode() == DisplayServer.WindowMode.Fullscreen;
        toggleFullscreenButton.Text = isFullscreen ? "Выключить полноэкранный режим" : "Включить полноэкранный режим";
        GD.Print($"Обновление текста кнопки: Текущий режим окна={displayManager.GetWindowMode()}, Текст кнопки={toggleFullscreenButton.Text}");
    }

    private void OnToggleFullscreenButtonPressed()
    {
        GD.Print("ToggleFullscreenButton pressed");
        bool isFullscreen = displayManager.GetWindowMode() == DisplayServer.WindowMode.Fullscreen;
        if (isFullscreen)
        {
            GD.Print("Переключаем в оконный режим");
            displayManager.SetWindowMode(DisplayServer.WindowMode.Windowed);
            displayManager.SetBorderless(false);
        }
        else
        {
            GD.Print("Переключаем в полноэкранный режим");
            displayManager.SetWindowMode(DisplayServer.WindowMode.Fullscreen);
        }
        // Обновляем текст кнопки после переключения
        UpdateFullscreenButtonText();
    }

    private void OnBackButtonPressed()
    {
        GD.Print("Back button pressed!");
        var global = GetNode<Global>("/root/Global");
        global.PlayerNickname = nicknameInput.Text;
        global.SaveSettings();
        displayManager.SaveSettings();
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