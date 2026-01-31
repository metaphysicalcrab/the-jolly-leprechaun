using Godot;
using TheJollyLeprechaun.Core.Base;
using TheJollyLeprechaun.Core.Interfaces;
using TheJollyLeprechaun.Entities.Enemies;

namespace TheJollyLeprechaun.Components.StateMachine.States;

/// <summary>
/// Enemy chase state - pursues the player until caught or lost.
/// </summary>
public partial class EnemyChaseState : BaseState
{
    [Export] private NodePath _enemyPath = "../..";
    [Export] private float _catchDistance = 1.0f;

    private BaseEnemy? _enemy;
    private float _timeSinceLastSeen;

    public override void _Ready()
    {
        base._Ready();
        _enemy = GetNodeOrNull<BaseEnemy>(_enemyPath);
    }

    public override void Enter()
    {
        GD.Print($"EnemyChaseState.Enter() - _enemy={((_enemy != null) ? _enemy.Name : "NULL")}");

        if (_enemy == null)
        {
            GD.PrintErr("EnemyChaseState: _enemy is NULL! Cannot chase.");
            return;
        }

        _timeSinceLastSeen = 0;

        // Set initial target
        if (_enemy.CurrentTarget != null)
        {
            _enemy.LastKnownTargetPosition = _enemy.CurrentTarget.GlobalPosition;
            _enemy.SetNavigationTarget(_enemy.LastKnownTargetPosition);
            GD.Print($"EnemyChaseState: Set nav target to {_enemy.LastKnownTargetPosition}");
        }
        else
        {
            GD.PrintErr("EnemyChaseState: CurrentTarget is NULL!");
        }
    }

    public override void Update(double delta)
    {
        if (_enemy == null) return;

        // Update target position if we can see the player
        if (_enemy.CurrentTarget != null && CanSeeTarget())
        {
            _timeSinceLastSeen = 0;
            _enemy.LastKnownTargetPosition = _enemy.CurrentTarget.GlobalPosition;
            _enemy.SetNavigationTarget(_enemy.LastKnownTargetPosition);

            // Check for catch
            var distanceToTarget = _enemy.GlobalPosition.DistanceTo(_enemy.CurrentTarget.GlobalPosition);
            if (distanceToTarget <= _catchDistance)
            {
                _enemy.OnCaughtPlayer(_enemy.CurrentTarget);
            }
        }
        else
        {
            // Lost sight of target
            _timeSinceLastSeen += (float)delta;

            if (_timeSinceLastSeen >= _enemy.Data.LoseTargetTime)
            {
                _enemy.CurrentTarget = null;
                _enemy.OnPlayerLost();
            }
        }
    }

    private int _physicsUpdateCount;

    public override void PhysicsUpdate(double delta)
    {
        _physicsUpdateCount++;

        if (_enemy == null)
        {
            if (_physicsUpdateCount % 60 == 0)
                GD.PrintErr($"EnemyChaseState.PhysicsUpdate: _enemy is NULL!");
            return;
        }

        // Debug every 30 frames (about 2x per second at 60fps)
        if (_physicsUpdateCount % 30 == 0)
        {
            GD.Print($"{_enemy.Name} ChaseState.PhysicsUpdate - Pos={_enemy.GlobalPosition}, Target={_enemy.LastKnownTargetPosition}");
        }

        // Check if this is a bird that should fly
        if (_enemy is BirdEnemy bird)
        {
            // Update flight state and fly if needed
            if (bird.UpdateFlightState() || bird.IsFlying || bird.FlightPhase != "grounded")
            {
                bird.FlyTowardsTarget(delta);
                return;
            }
        }

        // Normal ground navigation
        _enemy.NavigateToTarget(_enemy.Data.ChaseSpeed);
    }

    private bool CanSeeTarget()
    {
        if (_enemy?.CurrentTarget == null) return false;

        // Check if target implements IDetectable and is hidden
        if (_enemy.CurrentTarget is IDetectable detectable && !detectable.CanBeDetected())
        {
            return false;
        }

        // Could add line-of-sight check here
        // For now, just check if target is within detection range
        var distance = _enemy.GlobalPosition.DistanceTo(_enemy.CurrentTarget.GlobalPosition);
        return distance <= _enemy.Data.DetectionRange;
    }
}
