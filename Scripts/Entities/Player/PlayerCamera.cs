using Godot;
using TheJollyLeprechaun.Core.Autoloads;

namespace TheJollyLeprechaun.Entities.Player;

/// <summary>
/// Third-person camera controller with orbit and follow behavior.
/// </summary>
public partial class PlayerCamera : Node3D
{
    [ExportGroup("Follow Settings")]
    [Export] private NodePath _targetPath = "..";
    [Export] private float _followSpeed = 10.0f;
    [Export] private Vector3 _offset = new(0, 1.2f, 0); // Head height for leprechaun

    [ExportGroup("Rotation Settings")]
    [Export] private float _minPitch = -80.0f;
    [Export] private float _maxPitch = 80.0f;
    [Export] private bool _invertY = false;

    [ExportGroup("Zoom Settings")]
    [Export] private float _minZoom = 2.0f;
    [Export] private float _maxZoom = 10.0f;
    [Export] private float _defaultZoom = 5.0f;
    [Export] private float _zoomStep = 0.5f;
    [Export] private float _zoomSpeed = 8.0f;
    [Export] private float _toggleZoomDistance = 2.5f; // Close zoom when toggled

    [ExportGroup("References")]
    [Export] private NodePath _springArmPath = "SpringArm3D";

    private Node3D? _target;
    private SpringArm3D? _springArm;
    private float _pitch;
    private float _yaw;
    private bool _enabled = true;
    private float _targetZoom;
    private bool _isZoomedIn;

    public bool IsEnabled => _enabled;

    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
    }

    public override void _Ready()
    {
        _target = GetNodeOrNull<Node3D>(_targetPath);
        _springArm = GetNodeOrNull<SpringArm3D>(_springArmPath);

        if (_target == null)
            GD.PrintErr("PlayerCamera: _target is null!");

        // Initialize rotation from current transform
        _yaw = Rotation.Y;
        _pitch = 0;

        // Initialize zoom
        _targetZoom = _defaultZoom;
        if (_springArm != null)
        {
            _springArm.SpringLength = _defaultZoom;
        }

        // Make camera top-level so it doesn't inherit player rotation
        TopLevel = true;

        if (_target != null)
        {
            GlobalPosition = _target.GlobalPosition + _offset;
        }
    }

    public override void _Process(double delta)
    {
        if (!_enabled) return;

        HandleCameraInput();
        HandleZoomInput();
        UpdateZoom(delta);
        FollowTarget(delta);
    }

    private void HandleCameraInput()
    {
        var cameraInput = InputManager.Instance?.GetCameraInput() ?? Vector2.Zero;

        if (cameraInput == Vector2.Zero) return;

        // Update yaw (horizontal rotation)
        _yaw -= cameraInput.X;

        // Update pitch (vertical rotation)
        var pitchDelta = cameraInput.Y * (_invertY ? -1 : 1);
        _pitch -= pitchDelta;
        _pitch = Mathf.Clamp(_pitch, Mathf.DegToRad(_minPitch), Mathf.DegToRad(_maxPitch));

        // Apply rotation
        Rotation = new Vector3(_pitch, _yaw, 0);
    }

    private void HandleZoomInput()
    {
        // Mouse scroll wheel zoom
        if (InputManager.Instance?.IsZoomInJustPressed() == true)
        {
            _targetZoom = Mathf.Max(_targetZoom - _zoomStep, _minZoom);
            _isZoomedIn = false; // Cancel toggle state when manually zooming
        }
        else if (InputManager.Instance?.IsZoomOutJustPressed() == true)
        {
            _targetZoom = Mathf.Min(_targetZoom + _zoomStep, _maxZoom);
            _isZoomedIn = false;
        }

        // Right stick click toggle zoom
        if (InputManager.Instance?.IsZoomToggleJustPressed() == true)
        {
            _isZoomedIn = !_isZoomedIn;
            _targetZoom = _isZoomedIn ? _toggleZoomDistance : _defaultZoom;
        }
    }

    private void UpdateZoom(double delta)
    {
        if (_springArm == null) return;

        // Smoothly interpolate to target zoom
        _springArm.SpringLength = Mathf.Lerp(
            _springArm.SpringLength,
            _targetZoom,
            _zoomSpeed * (float)delta
        );
    }

    private void FollowTarget(double delta)
    {
        if (_target == null) return;

        var targetPosition = _target.GlobalPosition + _offset;
        GlobalPosition = GlobalPosition.Lerp(targetPosition, _followSpeed * (float)delta);
    }
}
