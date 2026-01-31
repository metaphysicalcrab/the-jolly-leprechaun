using Godot;
using TheJollyLeprechaun.Core.Base;
using TheJollyLeprechaun.Entities.Enemies;

namespace TheJollyLeprechaun.Components.StateMachine.States;

/// <summary>
/// Enemy scared state - flees from the player, then recovers.
/// </summary>
public partial class EnemyScaredState : BaseState
{
    [Export] private NodePath _enemyPath = "../..";
    [Export] private float _fleeDistance = 15.0f;

    private BaseEnemy? _enemy;
    private Vector3 _fleeTarget;
    private float _recoveryTimer;
    private bool _reachedFleePoint;

    public override void _Ready()
    {
        base._Ready();
        _enemy = GetNodeOrNull<BaseEnemy>(_enemyPath);
    }

    public override void Enter()
    {
        if (_enemy == null) return;
        _reachedFleePoint = false;
        _recoveryTimer = _enemy.Data.ScareRecoveryTime;

        // Calculate flee target position
        _fleeTarget = _enemy.GlobalPosition + _enemy.FleeDirection * _fleeDistance;
        _enemy.SetNavigationTarget(_fleeTarget);
    }

    public override void Update(double delta)
    {
        if (_enemy == null) return;

        // Count down recovery after reaching flee point
        if (_reachedFleePoint)
        {
            _recoveryTimer -= (float)delta;

            if (_recoveryTimer <= 0)
            {
                _enemy.RecoverFromScare();
            }
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        if (_enemy == null) return;

        if (_reachedFleePoint)
        {
            // Stay in place while recovering
            _enemy.Velocity = Vector3.Zero;
            _enemy.MoveAndSlide();
            return;
        }

        // Check if we've reached the flee point
        if (_enemy.HasReachedTarget())
        {
            _reachedFleePoint = true;
            return;
        }

        // Flee at flee speed
        _enemy.NavigateToTarget(_enemy.Data.FleeSpeed);
    }
}
