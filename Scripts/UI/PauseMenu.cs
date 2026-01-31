using Godot;
using TheJollyLeprechaun.Core.Autoloads;

namespace TheJollyLeprechaun.UI;

/// <summary>
/// Pause menu with resume, main menu, and quit options.
/// </summary>
public partial class PauseMenu : CanvasLayer
{
    [ExportGroup("References")]
    [Export] private NodePath _containerPath = "CenterContainer/PanelContainer";
    [Export] private NodePath _resumeButtonPath = "CenterContainer/PanelContainer/VBoxContainer/ResumeButton";
    [Export] private NodePath _fullscreenCheckPath = "CenterContainer/PanelContainer/VBoxContainer/FullscreenCheck";
    [Export] private NodePath _mainMenuButtonPath = "CenterContainer/PanelContainer/VBoxContainer/MainMenuButton";
    [Export] private NodePath _quitButtonPath = "CenterContainer/PanelContainer/VBoxContainer/QuitButton";

    private Control? _container;
    private Button? _resumeButton;
    private CheckButton? _fullscreenCheck;
    private Button? _mainMenuButton;
    private Button? _quitButton;

    public override void _Ready()
    {
        _container = GetNodeOrNull<Control>(_containerPath);
        _resumeButton = GetNodeOrNull<Button>(_resumeButtonPath);
        _fullscreenCheck = GetNodeOrNull<CheckButton>(_fullscreenCheckPath);
        _mainMenuButton = GetNodeOrNull<Button>(_mainMenuButtonPath);
        _quitButton = GetNodeOrNull<Button>(_quitButtonPath);

        // Connect button signals
        _resumeButton?.Connect("pressed", Callable.From(OnResumePressed));
        _fullscreenCheck?.Connect("toggled", Callable.From<bool>(OnFullscreenToggled));
        _mainMenuButton?.Connect("pressed", Callable.From(OnMainMenuPressed));
        _quitButton?.Connect("pressed", Callable.From(OnQuitPressed));

        // Subscribe to game events
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.GamePaused += OnGamePaused;
            GameEvents.Instance.GameResumed += OnGameResumed;
        }

        // Start hidden
        Hide();
    }

    public override void _ExitTree()
    {
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.GamePaused -= OnGamePaused;
            GameEvents.Instance.GameResumed -= OnGameResumed;
        }
    }

    private void OnGamePaused()
    {
        // Update fullscreen checkbox to current state
        if (_fullscreenCheck != null)
        {
            _fullscreenCheck.ButtonPressed = DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen;
        }

        Show();
        _resumeButton?.GrabFocus();
    }

    private void OnGameResumed()
    {
        Hide();
    }

    private void OnResumePressed()
    {
        GameManager.Instance?.Resume();
    }

    private void OnFullscreenToggled(bool enabled)
    {
        DisplayServer.WindowSetMode(
            enabled ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed
        );
    }

    private void OnMainMenuPressed()
    {
        GameManager.Instance?.GoToMainMenu();
    }

    private void OnQuitPressed()
    {
        GameManager.Instance?.QuitGame();
    }

    private new void Show()
    {
        Visible = true;
        ProcessMode = ProcessModeEnum.Always; // Process while paused
    }

    private new void Hide()
    {
        Visible = false;
    }
}
