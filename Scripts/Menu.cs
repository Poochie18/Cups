using Godot;

public partial class Menu : Control
{
    private Button playVsFriendButton;
    private Button playVsBotButton;
    private Button easyButton;
    private Button mediumButton;
    private Button hardButton;
    private Button backButton;
    private VBoxContainer mainMenuContainer;
    private VBoxContainer difficultyMenu;
    private bool showingDifficulty = false;

    public override void _Ready()
    {
        // Получаем контейнеры из сцены
        mainMenuContainer = GetNode<VBoxContainer>("MainMenu");
        difficultyMenu = GetNode<VBoxContainer>("DifficultyMenu");

        // Получаем кнопки из MainMenu
        playVsFriendButton = GetNode<Button>("MainMenu/PlayVsFriendButton");
        playVsBotButton = GetNode<Button>("MainMenu/PlayVsBotButton");

        // Получаем кнопки из DifficultyMenu
        easyButton = GetNode<Button>("DifficultyMenu/EasyButton");
        mediumButton = GetNode<Button>("DifficultyMenu/MediumButton");
        hardButton = GetNode<Button>("DifficultyMenu/HardButton");
        backButton = GetNode<Button>("DifficultyMenu/BackButton");

        // Проверяем, что все узлы найдены
        if (mainMenuContainer == null || difficultyMenu == null || 
            playVsFriendButton == null || playVsBotButton == null ||
            easyButton == null || mediumButton == null || hardButton == null || backButton == null)
        {
            GD.PrintErr("Ошибка: Не найдены узлы меню!");
            GD.Print($"mainMenu: {mainMenuContainer}, difficultyMenu: {difficultyMenu}");
            GD.Print($"playVsFriend: {playVsFriendButton}, playVsBot: {playVsBotButton}");
            GD.Print($"easy: {easyButton}, medium: {mediumButton}, hard: {hardButton}, back: {backButton}");
            return;
        }

        // Привязываем события к кнопкам
        playVsFriendButton.Pressed += OnPlayVsFriendButtonPressed;
        playVsBotButton.Pressed += OnPlayVsBotButtonPressed;
        easyButton.Pressed += () => StartGame("bot", 0);
        mediumButton.Pressed += () => StartGame("bot", 1);
        hardButton.Pressed += () => StartGame("bot", 2);
        backButton.Pressed += OnBackButtonPressed;

        // Устанавливаем начальное состояние
        mainMenuContainer.Visible = true;
        difficultyMenu.Visible = false;
        showingDifficulty = false;

        GD.Print("Menu initialized. Screen size: ", GetViewportRect().Size);
    }

    private void OnPlayVsFriendButtonPressed()
    {
        GD.Print("Play vs Friend button pressed!");
        StartGame("friend");
    }

    private void OnPlayVsBotButtonPressed()
    {
        GD.Print("Play vs Bot button pressed!");
        showingDifficulty = true;
        mainMenuContainer.Visible = false;
        difficultyMenu.Visible = true;
    }

    private void OnBackButtonPressed()
    {
        GD.Print("Back button pressed!");
        showingDifficulty = false;
        mainMenuContainer.Visible = true;
        difficultyMenu.Visible = false;
    }

    private void StartGame(string mode, int difficulty = 0)
    {
        mainMenuContainer.Visible = false;
        difficultyMenu.Visible = false;

        PackedScene gameScene = GD.Load<PackedScene>("res://Scenes/Game.tscn");
        if (gameScene != null)
        {
            Game gameInstance = gameScene.Instantiate<Game>();
            gameInstance.SetGameMode(mode, difficulty);
            GetTree().Root.AddChild(gameInstance);
            QueueFree();
        }
        else
        {
            GD.PrintErr("Ошибка: Не удалось загрузить Game.tscn!");
        }
    }
}