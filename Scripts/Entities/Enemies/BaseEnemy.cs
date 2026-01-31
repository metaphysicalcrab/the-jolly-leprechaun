using Godot;
using TheJollyLeprechaun.Core.Autoloads;
using TheJollyLeprechaun.Core.Base;
using TheJollyLeprechaun.Core.Interfaces;
using TheJollyLeprechaun.Data;

namespace TheJollyLeprechaun.Entities.Enemies;

/// <summary>
/// Base class for all enemy types.
/// Provides common functionality and references.
/// </summary>
public abstract partial class BaseEnemy : CharacterBody3D, IScareable
{
    [ExportGroup("Configuration")]
    [Export] public EnemyData Data { get; set; } = null!;

    [ExportGroup("References")]
    [Export] private NodePath _stateMachinePath = "StateMachine";
    [Export] private NodePath _navAgentPath = "NavigationAgent3D";
    [Export] private NodePath _modelPath = "Model";

    [ExportGroup("Model")]
    [Export(PropertyHint.Range, "0,360,1")]
    private float _modelRotationOffset = 180f; // Offset in degrees (180 = model faces -Z by default)

    // Resolved references
    public StateMachine? StateMachine { get; private set; }
    public NavigationAgent3D? NavAgent { get; private set; }
    public Node3D? Model { get; private set; }

    // Patrol points - set in the editor or via code
    [Export] public Node3D[]? PatrolPoints { get; set; }

    // Current target (usually the player)
    public Node3D? CurrentTarget { get; set; }
    public Vector3 LastKnownTargetPosition { get; set; }

    // Logical facing direction (independent of model orientation)
    public Vector3 FacingDirection { get; private set; } = Vector3.Forward;

    // IScareable implementation
    private bool _isScared;
    private float _currentFearLevel;
    private Vector3 _fleeDirection;

    public bool IsScared => _isScared;
    public float CurrentFearLevel => _currentFearLevel;
    public Vector3 FleeDirection => _fleeDirection;

    public override void _Ready()
    {
        // Resolve node references from paths
        StateMachine = GetNodeOrNull<StateMachine>(_stateMachinePath);
        NavAgent = GetNodeOrNull<NavigationAgent3D>(_navAgentPath);
        Model = GetNodeOrNull<Node3D>(_modelPath);

        GD.Print($"{Name}: StateMachine={StateMachine != null}, NavAgent={NavAgent != null}, Model={Model != null}");

        // Subscribe to state changes
        if (StateMachine != null)
        {
            StateMachine.StateChanged += OnStateChanged;
        }

        // Check navigation setup after a frame (nav map needs time to initialize)
        CallDeferred(nameof(CheckNavigationSetup));
    }

    private void CheckNavigationSetup()
    {
        if (NavAgent == null)
        {
            GD.PrintErr($"{Name}: No NavigationAgent3D found!");
            return;
        }

        var navMap = NavAgent.GetNavigationMap();
        if (!navMap.IsValid)
        {
            GD.PrintErr($"{Name}: No valid navigation map! Make sure NavigationRegion3D has a baked NavigationMesh.");
        }
        else
        {
            GD.Print($"{Name}: Navigation map is valid");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // Apply gravity if not on floor
        if (!IsOnFloor())
        {
            Velocity += GetGravity() * (float)delta;
        }
    }

    public override void _ExitTree()
    {
        if (StateMachine != null)
        {
            StateMachine.StateChanged -= OnStateChanged;
        }
    }

    public void Scare(Node source, float intensity)
    {
        _currentFearLevel += intensity;

        if (_currentFearLevel >= Data.ScareThreshold && !_isScared)
        {
            _isScared = true;

            // Calculate flee direction (away from source)
            if (source is Node3D source3D)
            {
                _fleeDirection = (GlobalPosition - source3D.GlobalPosition).Normalized();
                _fleeDirection.Y = 0;
                _fleeDirection = _fleeDirection.Normalized();
            }

            GameEvents.Instance.EmitEnemyScared(this);

            // Transition to scared state
            StateMachine?.TransitionTo("ScaredState");
        }
    }

    public void RecoverFromScare()
    {
        _isScared = false;
        _currentFearLevel = 0;
        GameEvents.Instance.EmitEnemyRecovered(this);

        // Return to idle
        StateMachine?.TransitionTo("IdleState");
    }

    private int _navDebugCounter;

    /// <summary>Move towards the current navigation target.</summary>
    public void NavigateToTarget(float speed)
    {
        // Debug output every 30 frames (about 2x per second at 60fps)
        _navDebugCounter++;
        var shouldDebug = _navDebugCounter % 30 == 0 || _navDebugCounter == 1;

        if (NavAgent == null)
        {
            if (shouldDebug) GD.Print($"{Name}: NavigateToTarget - No NavAgent, using direct");
            MoveDirectlyTowards(LastKnownTargetPosition, speed);
            return;
        }

        // Get next position from navigation
        var nextPosition = NavAgent.GetNextPathPosition();
        var isFinished = NavAgent.IsNavigationFinished();
        var distToNext = GlobalPosition.DistanceTo(nextPosition);
        var isReachable = NavAgent.IsTargetReachable();

        if (shouldDebug)
        {
            GD.Print($"{Name}: Nav - Pos={GlobalPosition}, NextPos={nextPosition}, Target={NavAgent.TargetPosition}");
            GD.Print($"{Name}: Nav - IsFinished={isFinished}, DistToNext={distToNext:F2}, IsReachable={isReachable}, Speed={speed}");
        }

        // If navigation returns our current position, the path isn't working - use direct movement
        if (distToNext < 0.1f && !isFinished)
        {
            if (shouldDebug) GD.Print($"{Name}: Nav path invalid, falling back to direct movement");
            MoveDirectlyTowards(LastKnownTargetPosition, speed);
            return;
        }

        // Calculate direction
        var direction = GlobalPosition.DirectionTo(nextPosition);
        direction.Y = 0;
        direction = direction.Normalized();

        if (direction.LengthSquared() < 0.001f)
        {
            if (shouldDebug) GD.Print($"{Name}: Nav - Direction is zero, using direct movement");
            MoveDirectlyTowards(LastKnownTargetPosition, speed);
            return;
        }

        // Preserve Y velocity for gravity, set horizontal movement
        Velocity = new Vector3(direction.X * speed, Velocity.Y, direction.Z * speed);
        var posBefore = GlobalPosition;

        // Rotate towards movement direction
        RotateModelTowards(direction);

        MoveAndSlide();

        if (shouldDebug)
        {
            var moved = GlobalPosition.DistanceTo(posBefore);
            GD.Print($"{Name}: MoveAndSlide - Before={posBefore}, After={GlobalPosition}, Moved={moved:F3}m, Vel={Velocity}");
        }
    }

    /// <summary>Rotate the model to face a direction.</summary>
    protected void RotateModelTowards(Vector3 direction, double? deltaOverride = null)
    {
        if (Model == null) return;

        // Update the logical facing direction (used by detection)
        FacingDirection = direction.Normalized();

        var delta = deltaOverride ?? GetPhysicsProcessDeltaTime();
        var targetAngle = Mathf.Atan2(direction.X, direction.Z) + Mathf.DegToRad(_modelRotationOffset);
        var currentRotation = Model.Rotation;
        Model.Rotation = new Vector3(
            currentRotation.X,
            Mathf.LerpAngle(currentRotation.Y, targetAngle, Data.RotationSpeed * (float)delta),
            currentRotation.Z
        );
    }

    /// <summary>Move directly towards a position without navigation (fallback).</summary>
    public void MoveDirectlyTowards(Vector3 targetPosition, float speed)
    {
        var direction = GlobalPosition.DirectionTo(targetPosition);
        direction.Y = 0;
        direction = direction.Normalized();

        if (direction == Vector3.Zero)
        {
            return;
        }

        var posBefore = GlobalPosition;

        // Preserve Y velocity for gravity, set horizontal movement
        Velocity = new Vector3(direction.X * speed, Velocity.Y, direction.Z * speed);

        // Rotate towards movement direction
        RotateModelTowards(direction);

        MoveAndSlide();

        // Debug on first call and periodically
        if (_navDebugCounter % 30 == 0 || _navDebugCounter == 1)
        {
            var moved = GlobalPosition.DistanceTo(posBefore);
            GD.Print($"{Name}: DirectMove - Target={targetPosition}, Dir={direction}, Moved={moved:F3}m");
        }
    }

    /// <summary>Set the navigation destination.</summary>
    public void SetNavigationTarget(Vector3 position)
    {
        if (NavAgent == null)
        {
            GD.PrintErr($"{Name}: SetNavigationTarget - NavAgent is null!");
            return;
        }

        NavAgent.TargetPosition = position;
    }

    /// <summary>Check if we've reached the navigation target.</summary>
    public bool HasReachedTarget()
    {
        return NavAgent?.IsNavigationFinished() ?? true;
    }

    /// <summary>Called when player is detected. Override to customize behavior.</summary>
    public virtual void OnPlayerDetected(Node3D player)
    {
        CurrentTarget = player;
        LastKnownTargetPosition = player.GlobalPosition;
        GameEvents.Instance.EmitEnemyDetectedPlayer(this);

        GD.Print($"{Name}: OnPlayerDetected - StateMachine={StateMachine != null}, IsScared={IsScared}");

        if (!IsScared)
        {
            StateMachine?.TransitionTo("ChaseState");
        }
    }

    /// <summary>Called when player is lost. Override to customize behavior.</summary>
    public virtual void OnPlayerLost()
    {
        GameEvents.Instance.EmitEnemyLostPlayer(this);

        if (!IsScared)
        {
            StateMachine?.TransitionTo("IdleState");
        }
    }

    /// <summary>Called when colliding with player - triggers catch.</summary>
    public virtual void OnCaughtPlayer(Node3D player)
    {
        GD.Print($"{Name} caught the player!");
        GameEvents.Instance.EmitPlayerCaught(this);
    }

    private void OnStateChanged(string previousState, string newState)
    {
        GD.Print($"{Name}: {previousState} -> {newState}");
    }
}
