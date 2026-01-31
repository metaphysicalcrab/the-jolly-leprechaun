using Godot;

namespace TheJollyLeprechaun.Entities.Enemies;

/// <summary>
/// Bird enemy type.
/// Uses the bird_character model and has bird-specific behaviors.
/// Can fly to chase players on elevated platforms.
/// </summary>
public partial class BirdEnemy : BaseEnemy
{
    [ExportGroup("Flying Settings")]
    [Export] private float _flyHeightThreshold = 1.5f; // Height difference to trigger flying
    [Export] private float _flySpeed = 8.0f;
    [Export] private float _flyAcceleration = 5.0f;
    [Export] private float _landingDistance = 2.0f; // Distance to start landing
    [Export] private float _hoverHeight = 0.5f; // Height above target when flying

    [Signal] public delegate void FlightStateChangedEventHandler(bool isFlying, string flightPhase);

    public bool IsFlying { get; private set; }
    public string FlightPhase { get; private set; } = "grounded"; // grounded, takeoff, flying, landing

    private Vector3 _flyVelocity;
    private float _takeoffTimer;
    private float _landingTimer;
    private const float TakeoffDuration = 0.4f;
    private const float LandingDuration = 0.3f;

    public override void _Ready()
    {
        base._Ready();
        GD.Print($"BirdEnemy {Name} ready - can fly!");
    }

    /// <summary>
    /// Check if the target is at an elevated position that requires flying.
    /// </summary>
    public bool ShouldFlyToTarget()
    {
        if (CurrentTarget == null) return false;

        var heightDifference = CurrentTarget.GlobalPosition.Y - GlobalPosition.Y;
        return heightDifference > _flyHeightThreshold;
    }

    /// <summary>
    /// Start taking off to fly.
    /// </summary>
    public void StartTakeoff()
    {
        if (FlightPhase != "grounded") return;

        FlightPhase = "takeoff";
        _takeoffTimer = TakeoffDuration;
        EmitSignal(SignalName.FlightStateChanged, true, "takeoff");
        GD.Print($"{Name}: Taking off!");
    }

    /// <summary>
    /// Start landing sequence.
    /// </summary>
    public void StartLanding()
    {
        if (FlightPhase != "flying") return;

        FlightPhase = "landing";
        _landingTimer = LandingDuration;
        EmitSignal(SignalName.FlightStateChanged, true, "landing");
        GD.Print($"{Name}: Landing!");
    }

    /// <summary>
    /// Process flying movement directly toward target, ignoring nav mesh.
    /// </summary>
    public void FlyTowardsTarget(double delta)
    {
        if (CurrentTarget == null) return;

        // Handle takeoff animation phase
        if (FlightPhase == "takeoff")
        {
            _takeoffTimer -= (float)delta;
            // Rise up during takeoff
            Velocity = new Vector3(0, _flySpeed * 0.5f, 0);
            MoveAndSlide();

            if (_takeoffTimer <= 0)
            {
                FlightPhase = "flying";
                IsFlying = true;
                EmitSignal(SignalName.FlightStateChanged, true, "flying");
            }
            return;
        }

        // Handle landing animation phase
        if (FlightPhase == "landing")
        {
            _landingTimer -= (float)delta;
            // Descend during landing
            Velocity = new Vector3(Velocity.X * 0.5f, -_flySpeed * 0.3f, Velocity.Z * 0.5f);
            MoveAndSlide();

            if (_landingTimer <= 0 || IsOnFloor())
            {
                FlightPhase = "grounded";
                IsFlying = false;
                _flyVelocity = Vector3.Zero;
                EmitSignal(SignalName.FlightStateChanged, false, "grounded");
                GD.Print($"{Name}: Landed!");
            }
            return;
        }

        // Flying movement - direct 3D path to target
        var targetPos = CurrentTarget.GlobalPosition + new Vector3(0, _hoverHeight, 0);
        var direction = (targetPos - GlobalPosition).Normalized();

        // Accelerate towards target
        _flyVelocity = _flyVelocity.Lerp(direction * _flySpeed, _flyAcceleration * (float)delta);
        Velocity = _flyVelocity;

        // Rotate model to face movement direction
        var flatDirection = new Vector3(direction.X, 0, direction.Z).Normalized();
        if (flatDirection != Vector3.Zero)
        {
            RotateModelTowards(flatDirection, delta);
        }

        MoveAndSlide();

        // Check if we should land (target at similar height and close)
        var heightDiff = Mathf.Abs(CurrentTarget.GlobalPosition.Y - GlobalPosition.Y);
        var horizontalDist = new Vector2(
            GlobalPosition.X - CurrentTarget.GlobalPosition.X,
            GlobalPosition.Z - CurrentTarget.GlobalPosition.Z
        ).Length();

        if (heightDiff < _flyHeightThreshold * 0.5f && horizontalDist < _landingDistance)
        {
            StartLanding();
        }
    }

    /// <summary>
    /// Check if bird should transition from ground to flying during chase.
    /// </summary>
    public bool UpdateFlightState()
    {
        if (!IsFlying && ShouldFlyToTarget())
        {
            StartTakeoff();
            return true;
        }
        return IsFlying;
    }
}
