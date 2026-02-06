using Godot;
using TheJollyLeprechaun.Core.Autoloads;

namespace TheJollyLeprechaun.Entities.Player;

/// <summary>
/// Handles 3D platformer movement for the player character.
/// Camera-relative movement with jump and gravity.
/// </summary>
public partial class PlayerMovement : Node
{
    [ExportGroup("Movement")]
    [Export] private float _moveSpeed = 6.0f;
    [Export] private float _sprintMultiplier = 1.5f;
    [Export] private float _jumpVelocity = 8.0f;
    [Export] private float _rotationSpeed = 10.0f;

    [ExportGroup("Sneak")]
    [Export] private float _sneakSpeed = 2.5f;

    [ExportGroup("Double Jump")]
    [Export] private float _doubleJumpVelocity = 7.0f;
    [Export] private int _maxJumps = 2;

    [ExportGroup("Physics")]
    [Export] private float _gravity = 20.0f;
    [Export] private float _acceleration = 15.0f;
    [Export] private float _friction = 12.0f;
    [Export] private float _airControl = 0.3f;

    [ExportGroup("References")]
    [Export] private NodePath _bodyPath = "..";
    [Export] private NodePath _cameraPivotPath = "../CameraPivot";
    [Export] private NodePath _modelPath = "../Model";

    private CharacterBody3D? _body;
    private Node3D? _cameraPivot;
    private Node3D? _model;
    private bool _enabled = true;
    private bool _wasOnFloor;
    private bool _isSneaking;
    private int _jumpCount;

    [Signal] public delegate void JumpedEventHandler();
    [Signal] public delegate void LandedEventHandler();
    [Signal] public delegate void SneakToggledEventHandler(bool isSneaking);
    [Signal] public delegate void DoubleJumpedEventHandler();

    public bool IsEnabled => _enabled;
    public float MoveSpeed => _moveSpeed;
    public bool IsSneaking => _isSneaking;
    public int JumpCount => _jumpCount;

    public override void _Ready()
    {
        _body = GetNodeOrNull<CharacterBody3D>(_bodyPath);
        _cameraPivot = GetNodeOrNull<Node3D>(_cameraPivotPath);
        _model = GetNodeOrNull<Node3D>(_modelPath);

        GD.Print($"PlayerMovement._Ready(): _body={_body != null}, _cameraPivot={_cameraPivot != null}, _model={_model != null}");

        if (_body == null)
            GD.PrintErr("PlayerMovement: _body is null!");
        if (_cameraPivot == null)
            GD.PrintErr("PlayerMovement: _cameraPivot is null!");
    }

    private float _debugTimer = 0;

    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
        if (!enabled && _body != null)
        {
            // Stop movement when disabled
            _body.Velocity = new Vector3(0, _body.Velocity.Y, 0);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_enabled || _body == null) return;

        var velocity = _body.Velocity;
        var onFloor = _body.IsOnFloor();

        // Debug output every second
        _debugTimer += (float)delta;
        if (_debugTimer >= 1.0f)
        {
            _debugTimer = 0;
            var debugInput = InputManager.Instance?.GetMovementInput() ?? Vector2.Zero;
            GD.Print($"[Movement] pos={_body.GlobalPosition}, vel={velocity}, onFloor={onFloor}, input={debugInput}, InputMgr={InputManager.Instance != null}");
        }

        // Sneak toggle
        if (InputManager.Instance?.IsSneakJustPressed() == true && onFloor)
        {
            _isSneaking = !_isSneaking;
            EmitSignal(SignalName.SneakToggled, _isSneaking);
        }

        // Landing detection
        if (onFloor && !_wasOnFloor)
        {
            _jumpCount = 0;
            EmitSignal(SignalName.Landed);
        }
        _wasOnFloor = onFloor;

        // Apply gravity
        if (!onFloor)
        {
            velocity.Y -= _gravity * (float)delta;
        }

        // Jump (supports double jump)
        if (InputManager.Instance?.IsJumpJustPressed() == true && _jumpCount < _maxJumps)
        {
            var isDoubleJump = _jumpCount >= 1;
            velocity.Y = isDoubleJump ? _doubleJumpVelocity : _jumpVelocity;
            _jumpCount++;

            if (isDoubleJump)
            {
                EmitSignal(SignalName.DoubleJumped);
            }
            else
            {
                EmitSignal(SignalName.Jumped);
            }
        }

        // Get input and calculate movement direction relative to camera
        var inputDir = InputManager.Instance?.GetMovementInput() ?? Vector2.Zero;
        var direction = GetCameraRelativeDirection(inputDir);

        // Calculate current speed (sneak caps speed, sprint only when not sneaking)
        float currentSpeed;
        if (_isSneaking)
        {
            currentSpeed = _sneakSpeed;
        }
        else
        {
            var isSprinting = InputManager.Instance?.IsSprintPressed() == true;
            currentSpeed = isSprinting ? _moveSpeed * _sprintMultiplier : _moveSpeed;
        }

        // Apply movement
        var currentAccel = onFloor ? _acceleration : _acceleration * _airControl;
        var currentFriction = onFloor ? _friction : _friction * _airControl;

        if (direction != Vector3.Zero)
        {
            velocity.X = Mathf.MoveToward(velocity.X, direction.X * currentSpeed, currentAccel * (float)delta);
            velocity.Z = Mathf.MoveToward(velocity.Z, direction.Z * currentSpeed, currentAccel * (float)delta);

            // Rotate model to face movement direction
            RotateTowardsDirection(direction, delta);
        }
        else
        {
            // Apply friction when no input
            velocity.X = Mathf.MoveToward(velocity.X, 0, currentFriction * (float)delta);
            velocity.Z = Mathf.MoveToward(velocity.Z, 0, currentFriction * (float)delta);
        }

        _body.Velocity = velocity;
        _body.MoveAndSlide();
    }

    private Vector3 GetCameraRelativeDirection(Vector2 inputDir)
    {
        if (inputDir == Vector2.Zero || _cameraPivot == null)
            return Vector3.Zero;

        // Get camera's forward and right vectors (flattened to XZ plane)
        var cameraForward = -_cameraPivot.GlobalTransform.Basis.Z;
        cameraForward.Y = 0;
        cameraForward = cameraForward.Normalized();

        var cameraRight = _cameraPivot.GlobalTransform.Basis.X;
        cameraRight.Y = 0;
        cameraRight = cameraRight.Normalized();

        // Combine based on input (note: inputDir.Y is forward/backward, inverted)
        var direction = (cameraForward * -inputDir.Y + cameraRight * inputDir.X);
        return direction.Normalized();
    }

    private void RotateTowardsDirection(Vector3 direction, double delta)
    {
        if (_model == null || direction == Vector3.Zero) return;

        // Calculate target angle - add PI to flip 180 degrees since model faces +Z but we want -Z forward
        var targetAngle = Mathf.Atan2(direction.X, direction.Z) + Mathf.Pi;
        var currentRotation = _model.Rotation;

        _model.Rotation = new Vector3(
            currentRotation.X,
            Mathf.LerpAngle(currentRotation.Y, targetAngle, _rotationSpeed * (float)delta),
            currentRotation.Z
        );
    }
}
