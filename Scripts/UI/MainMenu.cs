using Godot;
using TheJollyLeprechaun.Core.Autoloads;

namespace TheJollyLeprechaun.UI;

/// <summary>
/// Main menu with start game and quit options.
/// </summary>
public partial class MainMenu : Control
{
    [ExportGroup("References")]
    [Export] private NodePath _startButtonPath = "CenterContainer/PanelContainer/VBoxContainer/StartButton";
    [Export] private NodePath _fullscreenCheckPath = "CenterContainer/PanelContainer/VBoxContainer/FullscreenCheck";
    [Export] private NodePath _quitButtonPath = "CenterContainer/PanelContainer/VBoxContainer/QuitButton";

    private Button? _startButton;
    private CheckButton? _fullscreenCheck;
    private Button? _quitButton;

    public override void _Ready()
    {
        _startButton = GetNodeOrNull<Button>(_startButtonPath);
        _fullscreenCheck = GetNodeOrNull<CheckButton>(_fullscreenCheckPath);
        _quitButton = GetNodeOrNull<Button>(_quitButtonPath);

        // Connect button signals
        _startButton?.Connect("pressed", Callable.From(OnStartPressed));
        _fullscreenCheck?.Connect("toggled", Callable.From<bool>(OnFullscreenToggled));
        _quitButton?.Connect("pressed", Callable.From(OnQuitPressed));

        // Initialize fullscreen checkbox to current state
        if (_fullscreenCheck != null)
        {
            _fullscreenCheck.ButtonPressed = DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen;
        }

        // Make sure mouse is visible in menu
        Input.MouseMode = Input.MouseModeEnum.Visible;

        // Focus start button
        _startButton?.GrabFocus();
    }

    private void OnStartPressed()
    {
        GameManager.Instance?.StartNewGame();
    }

    private void OnFullscreenToggled(bool enabled)
    {
        DisplayServer.WindowSetMode(
            enabled ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed
        );
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
