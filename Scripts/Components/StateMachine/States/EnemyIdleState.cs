using Godot;
using TheJollyLeprechaun.Core.Base;
using TheJollyLeprechaun.Entities.Enemies;

namespace TheJollyLeprechaun.Components.StateMachine.States;

/// <summary>
/// Enemy idle state - stands in place for a duration, then transitions to patrol.
/// </summary>
public partial class EnemyIdleState : BaseState
{
	[Export] private NodePath _enemyPath = "../..";
	private BaseEnemy? _enemy;

	private float _timer;

	public override void _Ready()
	{
		base._Ready();
		_enemy = GetNodeOrNull<BaseEnemy>(_enemyPath);
	}

	public override void Enter()
	{
		if (_enemy == null) return;
		_timer = _enemy.Data.IdleDuration;
		_enemy.Velocity = Vector3.Zero;
	}

	public override void Update(double delta)
	{
		if (_enemy == null) return;
		_timer -= (float)delta;

		if (_timer <= 0)
		{
			// Transition to patrol if we have patrol points
			if (_enemy.PatrolPoints != null && _enemy.PatrolPoints.Length > 0)
			{
				TransitionTo("PatrolState");
			}
			else
			{
				// No patrol points - reset idle timer
				_timer = _enemy.Data.IdleDuration;
			}
		}
	}

	public override void PhysicsUpdate(double delta)
	{
		if (_enemy == null) return;
		// Stay in place
		_enemy.Velocity = Vector3.Zero;
		_enemy.MoveAndSlide();
	}
}
