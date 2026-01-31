using Godot;

namespace TheJollyLeprechaun.Core.Autoloads;

/// <summary>
/// Manages game state, level loading, and pause functionality.
/// </summary>
public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; } = null!;

    [Export] private string _mainMenuPath = "res://Scenes/UI/Menus/MainMenu.tscn";
    [Export] private string _firstLevelPath = "res://Scenes/Levels/TestLevel.tscn";

    private string _currentLevelPath = "";
    private bool _isPaused;
    private bool _isRestarting;

    public bool IsPaused => _isPaused;

    public override void _Ready()
    {
        Instance = this;

        // Subscribe to game events
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.PlayerCaught += OnPlayerCaught;
            GameEvents.Instance.LevelRestartRequested += OnLevelRestartRequested;
            GameEvents.Instance.GamePaused += OnGamePaused;
            GameEvents.Instance.GameResumed += OnGameResumed;
        }

        // Subscribe to input pause (defer to ensure InputManager is ready)
        CallDeferred(nameof(SubscribeToInput));

        GD.Print("GameManager: Ready");
    }

    private void SubscribeToInput()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.PausePressed += OnPausePressed;
            GD.Print("GameManager: Subscribed to InputManager.PausePressed");
        }
        else
        {
            GD.PrintErr("GameManager: InputManager.Instance is null!");
        }
    }

    public override void _ExitTree()
    {
        // Unsubscribe from events
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.PlayerCaught -= OnPlayerCaught;
            GameEvents.Instance.LevelRestartRequested -= OnLevelRestartRequested;
            GameEvents.Instance.GamePaused -= OnGamePaused;
            GameEvents.Instance.GameResumed -= OnGameResumed;
        }

        if (InputManager.Instance != null)
        {
            InputManager.Instance.PausePressed -= OnPausePressed;
        }
    }

    /// <summary>Load and switch to a new level.</summary>
    public void LoadLevel(string levelPath)
    {
        _currentLevelPath = levelPath;
        _isRestarting = false;
        InputManager.Instance.CaptureMouse();
        GetTree().ChangeSceneToFile(levelPath);
    }

    /// <summary>Restart the current level.</summary>
    public void RestartCurrentLevel()
    {
        if (string.IsNullOrEmpty(_currentLevelPath))
        {
            GD.PrintErr("GameManager: No current level to restart");
            return;
        }

        GetTree().ReloadCurrentScene();
    }

    /// <summary>Go to the main menu.</summary>
    public void GoToMainMenu()
    {
        Resume();
        GetTree().ChangeSceneToFile(_mainMenuPath);
    }

    /// <summary>Start a new game from the first level.</summary>
    public void StartNewGame()
    {
        LoadLevel(_firstLevelPath);
    }

    /// <summary>Pause the game.</summary>
    public void Pause()
    {
        _isPaused = true;
        GetTree().Paused = true;
        InputManager.Instance.ReleaseMouse();
        GameEvents.Instance.EmitGamePaused();
    }

    /// <summary>Resume the game.</summary>
    public void Resume()
    {
        _isPaused = false;
        GetTree().Paused = false;
        InputManager.Instance.CaptureMouse();
        GameEvents.Instance.EmitGameResumed();
    }

    /// <summary>Toggle pause state.</summary>
    public void TogglePause()
    {
        if (_isPaused)
            Resume();
        else
            Pause();
    }

    /// <summary>Quit the game.</summary>
    public void QuitGame()
    {
        GetTree().Quit();
    }

    // Event handlers
    private void OnPlayerCaught(Node enemy)
    {
        // Prevent multiple restarts
        if (_isRestarting) return;
        _isRestarting = true;

        GD.Print($"Player caught by {enemy.Name}! Restarting level...");

        // Brief delay before restart for feedback
        GetTree().CreateTimer(0.5).Timeout += () =>
        {
            _isRestarting = false;
            RestartCurrentLevel();
        };
    }

    private void OnLevelRestartRequested()
    {
        RestartCurrentLevel();
    }

    private void OnGamePaused()
    {
        GD.Print("Game paused");
    }

    private void OnGameResumed()
    {
        GD.Print("Game resumed");
    }

    private void OnPausePressed()
    {
        GD.Print("GameManager: Pause pressed");
        TogglePause();
    }

    /// <summary>Set the current level path (called when level loads itself).</summary>
    public void SetCurrentLevel(string levelPath)
    {
        _currentLevelPath = levelPath;
    }
}
