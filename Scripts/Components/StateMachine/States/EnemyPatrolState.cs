using Godot;
using TheJollyLeprechaun.Core.Base;
using TheJollyLeprechaun.Entities.Enemies;

namespace TheJollyLeprechaun.Components.StateMachine.States;

/// <summary>
/// Enemy patrol state - moves between patrol points, waiting briefly at each.
/// </summary>
public partial class EnemyPatrolState : BaseState
{
    [Export] private NodePath _enemyPath = "../..";
    private BaseEnemy? _enemy;

    private int _currentPointIndex;
    private float _waitTimer;
    private bool _waiting;

    public override void _Ready()
    {
        base._Ready();
        _enemy = GetNodeOrNull<BaseEnemy>(_enemyPath);
    }

    public override void Enter()
    {
        _waiting = false;
        _waitTimer = 0;
        SetNextPatrolPoint();
    }

    public override void Update(double delta)
    {
        if (_waiting)
        {
            _waitTimer -= (float)delta;
            if (_waitTimer <= 0)
            {
                _waiting = false;
                SetNextPatrolPoint();
            }
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        if (_enemy == null) return;

        if (_waiting)
        {
            _enemy.Velocity = Vector3.Zero;
            _enemy.MoveAndSlide();
            return;
        }

        // Check if we've reached the current patrol point
        if (_enemy.HasReachedTarget())
        {
            _waiting = true;
            _waitTimer = _enemy.Data.PatrolWaitTime;
            return;
        }

        // Move towards patrol point
        _enemy.NavigateToTarget(_enemy.Data.WalkSpeed);
    }

    private void SetNextPatrolPoint()
    {
        if (_enemy == null)
        {
            TransitionTo("IdleState");
            return;
        }

        var patrolPoints = _enemy.PatrolPoints;
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            TransitionTo("IdleState");
            return;
        }

        _currentPointIndex = (_currentPointIndex + 1) % patrolPoints.Length;
        var targetPoint = patrolPoints[_currentPointIndex];

        if (targetPoint != null)
        {
            _enemy.SetNavigationTarget(targetPoint.GlobalPosition);
        }
    }
}
