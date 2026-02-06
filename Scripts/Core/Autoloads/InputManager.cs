using Godot;

namespace TheJollyLeprechaun.Core.Autoloads;

/// <summary>
/// Global input manager that abstracts keyboard+mouse and gamepad input.
/// Provides a unified interface for all input queries.
/// </summary>
public partial class InputManager : Node
{
    public static InputManager Instance { get; private set; } = null!;

    [ExportGroup("Mouse Settings")]
    [Export] private float _mouseSensitivity = 0.002f;

    [ExportGroup("Gamepad Settings")]
    [Export] private float _gamepadSensitivity = 3.0f;
    [Export] private float _gamepadDeadzone = 0.15f;

    private Vector2 _mouseMotion = Vector2.Zero;
    private bool _mouseCapured = true;

    // Signals for event-driven input (optional, can also poll)
    [Signal] public delegate void JumpPressedEventHandler();
    [Signal] public delegate void ScarePressedEventHandler();
    [Signal] public delegate void InteractPressedEventHandler();
    [Signal] public delegate void SneakPressedEventHandler();
    [Signal] public delegate void PausePressedEventHandler();

    public override void _Ready()
    {
        Instance = this;
        CaptureMouse();
        GD.Print("InputManager: Ready and Instance set");
    }

    public override void _Input(InputEvent @event)
    {
        // Accumulate mouse motion for camera
        if (@event is InputEventMouseMotion mouseMotion)
        {
            _mouseMotion += mouseMotion.Relative;
        }

        // Toggle mouse capture with Escape (also triggers pause)
        if (@event.IsActionPressed("pause"))
        {
            EmitSignal(SignalName.PausePressed);
        }

        // Emit signals for one-shot actions
        if (@event.IsActionPressed("jump"))
        {
            EmitSignal(SignalName.JumpPressed);
        }

        if (@event.IsActionPressed("scare"))
        {
            EmitSignal(SignalName.ScarePressed);
        }

        if (@event.IsActionPressed("interact"))
        {
            EmitSignal(SignalName.InteractPressed);
        }

        if (@event.IsActionPressed("sneak"))
        {
            EmitSignal(SignalName.SneakPressed);
        }
    }

    /// <summary>
    /// Get movement input as a normalized Vector2.
    /// X = right/left, Y = forward/backward (note: forward is negative Y in Godot's input system).
    /// </summary>
    public Vector2 GetMovementInput()
    {
        var input = Input.GetVector(
            "move_left", "move_right",
            "move_forward", "move_backward"
        );

        // Apply deadzone for gamepad
        if (input.Length() < _gamepadDeadzone)
        {
            return Vector2.Zero;
        }

        return input.Normalized();
    }

    /// <summary>
    /// Get camera input as a Vector2.
    /// Combines mouse motion (consumed after read) with gamepad right stick.
    /// X = horizontal rotation, Y = vertical rotation.
    /// </summary>
    public Vector2 GetCameraInput()
    {
        var result = Vector2.Zero;

        // Mouse input (consumed after reading)
        result += _mouseMotion * _mouseSensitivity;
        _mouseMotion = Vector2.Zero;

        // Gamepad input (continuous)
        var gamepadInput = Input.GetVector(
            "camera_left", "camera_right",
            "camera_up", "camera_down"
        );

        if (gamepadInput.Length() > _gamepadDeadzone)
        {
            result += gamepadInput * _gamepadSensitivity * (float)GetProcessDeltaTime();
        }

        return result;
    }

    // Polling methods for checking input state
    public bool IsJumpPressed() => Input.IsActionPressed("jump");
    public bool IsJumpJustPressed() => Input.IsActionJustPressed("jump");
    public bool IsScareJustPressed() => Input.IsActionJustPressed("scare");
    public bool IsInteractJustPressed() => Input.IsActionJustPressed("interact");
    public bool IsPauseJustPressed() => Input.IsActionJustPressed("pause");
    public bool IsSprintPressed() => Input.IsActionPressed("sprint");
    public bool IsSneakJustPressed() => Input.IsActionJustPressed("sneak");

    // Zoom input
    public bool IsZoomInJustPressed() => Input.IsActionJustPressed("zoom_in");
    public bool IsZoomOutJustPressed() => Input.IsActionJustPressed("zoom_out");
    public bool IsZoomToggleJustPressed() => Input.IsActionJustPressed("zoom_toggle");

    /// <summary>Capture the mouse cursor for gameplay.</summary>
    public void CaptureMouse()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _mouseCapured = true;
    }

    /// <summary>Release the mouse cursor for menus.</summary>
    public void ReleaseMouse()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _mouseCapured = false;
    }

    /// <summary>Toggle mouse capture state.</summary>
    public void ToggleMouseCapture()
    {
        if (_mouseCapured)
            ReleaseMouse();
        else
            CaptureMouse();
    }

    public bool IsMouseCaptured => _mouseCapured;
}
