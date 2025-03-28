using Godot;

public partial class Settings : Control
{
    private Button backButton;
    private LineEdit nicknameInput;
    private Button toggleFullscreenButton;
    private DisplayManager displayManager;

    public override void _Ready()
    {
        backButton = GetNode<Button>("SettingsContainer/BackButton");
        nicknameInput = GetNode<LineEdit>("SettingsContainer/NicknameInput");
        toggleFullscreenButton = GetNode<Button>("SettingsContainer/ToggleFullscreenButton");
        displayManager = GetNode<DisplayManager>("/root/DisplayManager");

        if (backButton == null || nicknameInput == null || toggleFullscreenButton == null || displayManager == null)
        {
            //GD.PrintErr("Error: Nodes not found in Settings!");
            //GD.Print($"backButton: {backButton}, nicknameInput: {nicknameInput}, toggleFullscreenButton: {toggleFullscreenButton}, displayManager: {displayManager}");
            return;
        }

        var global = GetNode<Global>("/root/Global");
        nicknameInput.Text = string.IsNullOrEmpty(global.PlayerNickname) ? "Player" : global.PlayerNickname;

        UpdateFullscreenButtonText();

        toggleFullscreenButton.Pressed += OnToggleFullscreenButtonPressed;

        backButton.Pressed += () =>
        {
            //GD.Print("BackButton pressed");
            OnBackButtonPressed();
        };
        //GD.Print("Settings scene initialized");
    }

    private void UpdateFullscreenButtonText()
    {
        bool isFullscreen = displayManager.GetWindowMode() == DisplayServer.WindowMode.Fullscreen;
        toggleFullscreenButton.Text = isFullscreen ? "Turn off fullscreen" : "Turn on fullscreen";
        //GD.Print($"Updating button text: Current window mode={displayManager.GetWindowMode()}, Button text={toggleFullscreenButton.Text}");
    }

    private void OnToggleFullscreenButtonPressed()
    {
        //GD.Print("ToggleFullscreenButton pressed");
        bool isFullscreen = displayManager.GetWindowMode() == DisplayServer.WindowMode.Fullscreen;
        if (isFullscreen)
        {
            //GD.Print("Switching to windowed mode");
            displayManager.SetWindowMode(DisplayServer.WindowMode.Windowed);
            displayManager.SetBorderless(false);
        }
        else
        {
            //GD.Print("Switching to fullscreen mode");
            displayManager.SetWindowMode(DisplayServer.WindowMode.Fullscreen);
        }
        UpdateFullscreenButtonText();
    }

    private void OnBackButtonPressed()
    {
        //GD.Print("Back button pressed!");
        var global = GetNode<Global>("/root/Global");
        global.PlayerNickname = nicknameInput.Text;
        global.SaveSettings();
        displayManager.SaveSettings();
        LoadScene("res://Scenes/Menu.tscn");
    }

    private void LoadScene(string path)
    {
        //GD.Print($"Attempting to load scene: {path}");
        PackedScene scene = GD.Load<PackedScene>(path);
        if (scene != null)
        {
            Node sceneInstance = scene.Instantiate();
            GetTree().Root.AddChild(sceneInstance);
            //GD.Print($"Scene {path} loaded and added to tree");
            QueueFree();
        }
        else
        {
            //GD.PrintErr($"Error: Failed to load scene {path}!");
        }
    }
}