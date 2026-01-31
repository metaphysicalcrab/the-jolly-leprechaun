using Godot;
using System.Linq;
using TheJollyLeprechaun.Core.Interfaces;
using TheJollyLeprechaun.Entities.Enemies;

namespace TheJollyLeprechaun.Components;

/// <summary>
/// Detects targets within a vision cone.
/// Attach as a child Area3D to an enemy.
/// </summary>
public partial class DetectionComponent : Area3D
{
    [ExportGroup("Detection Settings")]
    [Export] private float _detectionRange = 10.0f;
    [Export] private float _detectionAngle = 45.0f; // Half-angle in degrees
    [Export] private float _detectionInterval = 0.1f;

    [ExportGroup("Line of Sight")]
    [Export] private bool _requireLineOfSight = true;
    [Export] private uint _visionCollisionMask = 1;

    [ExportGroup("References")]
    [Export] private BaseEnemy? _enemy;

    [Signal] public delegate void TargetDetectedEventHandler(Node3D target);
    [Signal] public delegate void TargetLostEventHandler(Node3D target);

    public Node3D? CurrentTarget { get; private set; }
    public bool HasTarget => CurrentTarget != null;

    private float _detectionTimer;

    public override void _Ready()
    {
        // Try to find parent enemy if not set
        if (_enemy == null)
        {
            var parent = GetParent();
            while (parent != null)
            {
                if (parent is BaseEnemy enemy)
                {
                    _enemy = enemy;
                    break;
                }
                parent = parent.GetParent();
            }
        }

        GD.Print($"DetectionComponent ({_enemy?.Name}): Ready - Range={_detectionRange}, Angle={_detectionAngle}, Monitoring={Monitoring}");

        // Debug: log when bodies enter/exit
        BodyEntered += (body) => GD.Print($"DetectionComponent ({_enemy?.Name}): Body entered: {body.Name}");
        BodyExited += (body) => GD.Print($"DetectionComponent ({_enemy?.Name}): Body exited: {body.Name}");
    }

    public override void _PhysicsProcess(double delta)
    {
        _detectionTimer -= (float)delta;
        if (_detectionTimer <= 0)
        {
            _detectionTimer = _detectionInterval;
            PerformDetection();
        }
    }

    private void PerformDetection()
    {
        var overlappingBodies = GetOverlappingBodies();

        foreach (var body in overlappingBodies)
        {
            // Skip if not a Node3D
            if (body is not Node3D target3D) continue;

            // Check if implements IDetectable
            if (body is IDetectable detectable)
            {
                // Skip if hidden
                if (!detectable.CanBeDetected())
                {
                    GD.Print($"Detection ({_enemy?.Name}): Target {target3D.Name} is hidden");
                    continue;
                }
            }

            // Check if we can detect this target
            if (CanDetect(target3D))
            {
                if (CurrentTarget == null)
                {
                    GD.Print($"Detection ({_enemy?.Name}): Detected {target3D.Name}!");
                    CurrentTarget = target3D;
                    EmitSignal(SignalName.TargetDetected, target3D);
                    _enemy?.OnPlayerDetected(target3D);
                }
                return;
            }
        }

        // No target found
        if (CurrentTarget != null)
        {
            GD.Print($"Detection ({_enemy?.Name}): Lost target");
            var lostTarget = CurrentTarget;
            CurrentTarget = null;
            EmitSignal(SignalName.TargetLost, lostTarget);
            _enemy?.OnPlayerLost();
        }
    }

    private bool CanDetect(Node3D target)
    {
        var toTarget = target.GlobalPosition - GlobalPosition;
        var distance = toTarget.Length();

        // Range check
        if (distance > _detectionRange)
        {
            // GD.Print($"Detection ({_enemy?.Name}): {target.Name} out of range ({distance:F1} > {_detectionRange})");
            return false;
        }

        // Angle check (vision cone) - use enemy's logical facing direction
        Vector3 forward;
        if (_enemy != null)
        {
            // Use the enemy's logical facing direction (set by movement code)
            forward = _enemy.FacingDirection;
        }
        else
        {
            forward = -GlobalTransform.Basis.Z;
        }
        var angleToTarget = Mathf.RadToDeg(forward.AngleTo(toTarget.Normalized()));
        if (angleToTarget > _detectionAngle)
        {
            // GD.Print($"Detection ({_enemy?.Name}): {target.Name} outside vision cone (angle={angleToTarget:F1} > {_detectionAngle})");
            return false;
        }

        // Line of sight check
        if (_requireLineOfSight)
        {
            var spaceState = GetWorld3D().DirectSpaceState;

            // Raycast from eye level (not ground level)
            var eyeOffset = new Vector3(0, 0.8f, 0);
            var startPos = GlobalPosition + eyeOffset;
            var endPos = target.GlobalPosition + eyeOffset;

            var query = PhysicsRayQueryParameters3D.Create(
                startPos,
                endPos,
                _visionCollisionMask
            );

            // Exclude self and parent from raycast
            query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
            if (_enemy != null)
            {
                query.Exclude.Add(_enemy.GetRid());
            }

            var result = spaceState.IntersectRay(query);
            if (result.Count > 0)
            {
                var hitCollider = result["collider"].AsGodotObject();
                if (hitCollider != target)
                {
                    GD.Print($"Detection ({_enemy?.Name}): LOS blocked by {(hitCollider as Node)?.Name ?? "unknown"}");
                    return false; // Something is blocking line of sight
                }
            }
        }

        return true;
    }

    /// <summary>Force an immediate detection check.</summary>
    public void ForceDetection()
    {
        _detectionTimer = 0;
    }
}
