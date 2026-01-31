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
    [Export] private NodePath _quitButtonPath = "CenterContainer/PanelContainer/VBoxContainer/QuitButton";

    private Button? _startButton;
    private Button? _quitButton;

    public override void _Ready()
    {
        _startButton = GetNodeOrNull<Button>(_startButtonPath);
        _quitButton = GetNodeOrNull<Button>(_quitButtonPath);

        // Connect button signals
        _startButton?.Connect("pressed", Callable.From(OnStartPressed));
        _quitButton?.Connect("pressed", Callable.From(OnQuitPressed));

        // Make sure mouse is visible in menu
        Input.MouseMode = Input.MouseModeEnum.Visible;

        // Focus start button
        _startButton?.GrabFocus();
    }

    private void OnStartPressed()
    {
        GameManager.Instance?.StartNewGame();
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
